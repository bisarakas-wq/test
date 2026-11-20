namespace Server_ProductsApi.Models;

public sealed class AdminAccount
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
