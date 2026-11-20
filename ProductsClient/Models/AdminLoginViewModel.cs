using System.ComponentModel.DataAnnotations;

namespace ProductsClient.Models;

public sealed class AdminLoginViewModel
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string Password { get; set; } = string.Empty;

    public string? Error { get; set; }
}
