namespace Server_ProductsApi.Models;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";
    public string JwtKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Kadr.Booking.Api";
    public string Audience { get; set; } = "Kadr.Booking.Client";
}
