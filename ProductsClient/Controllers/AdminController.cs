using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductsClient.Models;

namespace ProductsClient.Controllers;

public sealed class AdminController(
    IHttpClientFactory httpClientFactory,
    ILogger<AdminController> logger) : Controller
{
    [HttpGet("/admin/login")]
    public IActionResult Login() => User.Identity?.IsAuthenticated == true
        ? RedirectToAction(nameof(Bookings))
        : View(new AdminLoginViewModel());

    [HttpPost("/admin/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Password = string.Empty;
            return View(model);
        }

        try
        {
            using var response = await httpClientFactory.CreateClient("ProductsApi")
                .PostAsJsonAsync("api/auth/login", new { model.Email, model.Password }, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                model.Error = "Неверный логин или пароль.";
                model.Password = string.Empty;
                return View(model);
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError("Authentication API returned HTTP {StatusCode}", (int)response.StatusCode);
                model.Error = "Не удалось выполнить вход. Попробуйте позже.";
                model.Password = string.Empty;
                return View(model);
            }

            var login = await response.Content.ReadFromJsonAsync<ApiLoginResponse>(cancellationToken);
            if (login is null || string.IsNullOrWhiteSpace(login.AccessToken) || login.ExpiresAtUtc <= DateTime.UtcNow)
            {
                logger.LogError("Authentication API returned an invalid login response");
                model.Error = "Не удалось выполнить вход. Попробуйте позже.";
                model.Password = string.Empty;
                return View(model);
            }

            var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, model.Email.Trim()),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("api_access_token", login.AccessToken)
            ],
            CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = login.ExpiresAtUtc,
                    AllowRefresh = false
                });

            return RedirectToAction(nameof(Bookings));
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Authentication API is unavailable");
            model.Error = "Вход временно недоступен. Попробуйте позже.";
            model.Password = string.Empty;
            return View(model);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Authentication API timed out");
            model.Error = "Вход временно недоступен. Попробуйте позже.";
            model.Password = string.Empty;
            return View(model);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("/admin/bookings")]
    public IActionResult Bookings() => View();

    [Authorize(Roles = "Admin")]
    [HttpPost("/admin/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private sealed record ApiLoginResponse(string AccessToken, DateTime ExpiresAtUtc);
}
