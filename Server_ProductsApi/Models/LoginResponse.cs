namespace Server_ProductsApi.Models;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
