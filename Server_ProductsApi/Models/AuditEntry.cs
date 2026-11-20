namespace Server_ProductsApi.Models;

public sealed class AuditEntry
{
    public long Id { get; set; }
    public required string Action { get; set; }
    public string? Actor { get; set; }
    public int? BookingId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
