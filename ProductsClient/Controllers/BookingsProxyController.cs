using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProductsClient.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsProxyController(
    IHttpClientFactory httpClientFactory,
    ILogger<BookingsProxyController> logger) : ControllerBase
{
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        await ForwardAsync(async client => await client.GetAsync("api/bookings", cancellationToken), cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken) =>
        await ForwardAsync(async client => await client.GetAsync($"api/bookings/{id}", cancellationToken), cancellationToken);

    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] JsonElement request, CancellationToken cancellationToken) =>
        await ForwardAsync(
            async client => await client.PostAsJsonAsync("api/bookings", request, cancellationToken), cancellationToken);

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken) =>
        await ForwardAsync(async client => await client.DeleteAsync($"api/bookings/{id}", cancellationToken), cancellationToken);

    private async Task<IActionResult> ForwardAsync(
        Func<HttpClient, Task<HttpResponseMessage>> send,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ProductsApi");
            if (User.Identity?.IsAuthenticated == true)
            {
                var token = User.FindFirst("api_access_token")?.Value;
                if (string.IsNullOrWhiteSpace(token))
                {
                    return Unauthorized();
                }

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await send(client);
            if ((int)response.StatusCode >= StatusCodes.Status500InternalServerError)
            {
                logger.LogError("Booking API returned HTTP {StatusCode} for {Path}", (int)response.StatusCode, Request.Path);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = content,
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }
        catch (HttpRequestException exception)
        {
            logger.LogError(exception, "Booking API is unavailable for {Path}", Request.Path);
            return StatusCode((int)HttpStatusCode.ServiceUnavailable, new ProblemDetails
            {
                Title = "Заявки временно недоступны",
                Detail = "Попробуйте повторить действие немного позже."
            });
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Booking API timed out for {Path}", Request.Path);
            return StatusCode((int)HttpStatusCode.GatewayTimeout, new ProblemDetails
            {
                Title = "Не удалось выполнить запрос",
                Detail = "Попробуйте повторить действие немного позже."
            });
        }
    }
}
