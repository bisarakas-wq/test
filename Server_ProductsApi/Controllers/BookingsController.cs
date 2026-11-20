using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server_ProductsApi.Data;
using Server_ProductsApi.Models;

namespace Server_ProductsApi.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(
    BookingDbContext database,
    TimeProvider timeProvider,
    ILogger<BookingsController> logger) : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, (int PricePerHour, int MinimumHours)> Rooms =
        new Dictionary<string, (int PricePerHour, int MinimumHours)>(StringComparer.Ordinal)
        {
            ["Мэн Кейв / Циклорама"] = (4000, 3),
            ["Лофт"] = (3000, 2),
            ["Кафе"] = (3000, 2),
            ["Соло"] = (3000, 2),
            ["Гримёрный стол"] = (500, 1)
        };

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BookingResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var bookings = await database.Bookings
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAtUtc)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);

        return Ok(bookings);
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookingResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var booking = await database.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return booking is null ? NotFound() : Ok(ToResponse(booking));
    }

    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(
        [FromBody] CreateBookingRequest request,
        CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        if (name.Length == 0)
        {
            ModelState.AddModelError(nameof(request.Name), "Укажите имя.");
            return ValidationProblem(ModelState);
        }

        if (!Rooms.TryGetValue(request.Room, out var room))
        {
            ModelState.AddModelError(nameof(request.Room), "Выбранный зал не найден.");
            return ValidationProblem(ModelState);
        }

        if (!DateOnly.TryParseExact(request.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            || !TimeOnly.TryParseExact(request.Time, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            ModelState.AddModelError(string.Empty, "Укажите корректные дату и время.");
            return ValidationProblem(ModelState);
        }

        if (request.Hours < room.MinimumHours || request.Hours > 12)
        {
            ModelState.AddModelError(nameof(request.Hours), $"Минимальная длительность для этого зала — {room.MinimumHours} ч.; максимум — 12 ч.");
            return ValidationProblem(ModelState);
        }

        if (time.Hour < 10 || time.Minute != 0 || time.Hour + request.Hours > 22)
        {
            ModelState.AddModelError(nameof(request.Time), "Бронирование возможно с 10:00 до 22:00, с шагом в один час.");
            return ValidationProblem(ModelState);
        }

        var startsAt = date.ToDateTime(time, DateTimeKind.Local);
        if (startsAt <= timeProvider.GetLocalNow().DateTime)
        {
            ModelState.AddModelError(nameof(request.Date), "Выберите дату и время в будущем.");
            return ValidationProblem(ModelState);
        }

        var booking = new Booking
        {
            Name = name,
            Room = request.Room,
            Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Time = time.ToString("HH:mm", CultureInfo.InvariantCulture),
            Hours = request.Hours,
            PricePerHour = room.PricePerHour,
            Total = checked(room.PricePerHour * request.Hours),
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        await using var transaction = await database.Database.BeginTransactionAsync(cancellationToken);
        database.Bookings.Add(booking);
        await database.SaveChangesAsync(cancellationToken);
        database.AuditEntries.Add(new AuditEntry
        {
            Action = "BookingCreated",
            BookingId = booking.Id,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation(
            "Booking {BookingId} created for room {Room}; duration {Hours} hours; total {Total}",
            booking.Id,
            booking.Room,
            booking.Hours,
            booking.Total);

        var response = ToResponse(booking);
        return CreatedAtAction(nameof(GetById), new { id = booking.Id }, response);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var booking = await database.Bookings.FindAsync([id], cancellationToken);
        if (booking is null)
        {
            return NotFound();
        }

        database.Bookings.Remove(booking);
        database.AuditEntries.Add(new AuditEntry
        {
            Action = "BookingDeleted",
            Actor = User.Identity?.Name,
            BookingId = id,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });
        await database.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Booking {BookingId} deleted by {Admin}", id, User.Identity?.Name);
        return NoContent();
    }

    private static BookingResponse ToResponse(Booking booking) => new(
        booking.Id,
        booking.Name,
        booking.Room,
        booking.Date,
        booking.Time,
        booking.Hours,
        booking.PricePerHour,
        booking.Total,
        booking.CreatedAtUtc);
}

public sealed class CreateBookingRequest
{
    [Required, StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Room { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
    public string Date { get; set; } = string.Empty;

    [Required, RegularExpression(@"^\d{2}:\d{2}$")]
    public string Time { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Hours { get; set; }
}

public sealed record BookingResponse(
    int Id,
    string Name,
    string Room,
    string Date,
    string Time,
    int Hours,
    int PricePerHour,
    int Total,
    DateTime CreatedAtUtc);
