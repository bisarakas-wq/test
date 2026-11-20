namespace Server_ProductsApi.Models;

public sealed class Booking
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Room { get; set; }
    public required string Date { get; set; }
    public required string Time { get; set; }
    public int Hours { get; set; }
    public int PricePerHour { get; set; }
    public int Total { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
