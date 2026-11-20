using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Server_ProductsApi.Data;
using Server_ProductsApi.Models;

namespace Server_ProductsApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    BookingDbContext database,
    IPasswordHasher<AdminAccount> passwordHasher,
    IOptions<AuthenticationOptions> authenticationOptions,
    TimeProvider timeProvider,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("admin-login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await database.Admins.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Email == email, cancellationToken);
        var validPassword = admin is not null
            && passwordHasher.VerifyHashedPassword(admin, admin.PasswordHash, request.Password)
                != PasswordVerificationResult.Failed;

        database.AuditEntries.Add(new AuditEntry
        {
            Action = validPassword ? "AdminLoginSucceeded" : "AdminLoginFailed",
            Actor = email,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        });
        await database.SaveChangesAsync(cancellationToken);

        if (!validPassword || admin is null)
        {
            logger.LogWarning("Administrator login failed for {Email}", email);
            return Unauthorized(new ProblemDetails
            {
                Title = "Не удалось войти",
                Detail = "Проверьте логин и пароль."
            });
        }

        var settings = authenticationOptions.Value;
        var expiresAt = timeProvider.GetUtcNow().AddHours(8);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(ClaimTypes.Name, admin.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.JwtKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            settings.Issuer,
            settings.Audience,
            claims,
            notBefore: timeProvider.GetUtcNow().UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        logger.LogInformation("Administrator {AdminId} logged in", admin.Id);
        return Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), expiresAt.UtcDateTime));
    }
}
