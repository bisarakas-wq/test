using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server_ProductsApi.Data;
using Server_ProductsApi.Models;
using Server_ProductsApi.Services;

var builder = WebApplication.CreateBuilder(args);

var databaseSetting = builder.Configuration["Database:Path"] ?? "App_Data/bookings.db";
var databasePath = Path.GetFullPath(databaseSetting, builder.Environment.ContentRootPath);
Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
var jwtKey = builder.Configuration["Authentication:JwtKey"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("Set Authentication:JwtKey to a secret of at least 32 bytes using user-secrets or an environment variable.");
}

var authOptions = builder.Configuration.GetSection(AuthenticationOptions.SectionName).Get<AuthenticationOptions>() ?? new AuthenticationOptions();
authOptions.JwtKey = jwtKey;
builder.Services.Configure<AuthenticationOptions>(builder.Configuration.GetSection(AuthenticationOptions.SectionName));
builder.Services.AddDbContext<BookingDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IPasswordHasher<AdminAccount>, PasswordHasher<AdminAccount>>();
builder.Services.AddHostedService<SqliteBackupService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = authOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ValidateLifetime = true,
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogError(exception, "Unhandled API error for {Path}", context.Request.Path);

    context.Response.Clear();
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Не удалось выполнить запрос.",
        Detail = "Повторите попытку позже."
    });
}));
app.UseStatusCodePages(async statusContext =>
{
    var context = statusContext.HttpContext;
    var statusCode = context.Response.StatusCode;
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogWarning("API returned HTTP {StatusCode} for {Path}", statusCode, context.Request.Path);
    context.Response.ContentType = "application/problem+json";
    await context.Response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = statusCode,
        Title = "Не удалось найти или выполнить запрос.",
        Detail = "Проверьте запрос и попробуйте снова."
    });
});

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    await database.Database.MigrateAsync();

    if (!await database.Admins.AnyAsync())
    {
        var email = builder.Configuration["Admin:Email"]?.Trim();
        var password = builder.Configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || password.Length < 14)
        {
            throw new InvalidOperationException(
                "No administrator exists. Configure Admin:Email and Admin:Password (at least 14 characters) using user-secrets or environment variables.");
        }

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AdminAccount>>();
        var admin = new AdminAccount
        {
            Email = email.ToLowerInvariant(),
            PasswordHash = string.Empty,
            CreatedAtUtc = app.Services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime
        };
        admin.PasswordHash = hasher.HashPassword(admin, password);
        database.Admins.Add(admin);
        database.AuditEntries.Add(new AuditEntry
        {
            Action = "AdminAccountCreated",
            Actor = admin.Email,
            CreatedAtUtc = admin.CreatedAtUtc
        });
        await database.SaveChangesAsync();
    }
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .ExcludeFromDescription();

app.MapControllers();
app.Logger.LogInformation("Booking API started with SQLite database at {DatabasePath}", databasePath);

app.Run();
