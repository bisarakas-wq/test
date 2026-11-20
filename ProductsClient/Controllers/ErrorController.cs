using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductsClient.Models;

namespace ProductsClient.Controllers;

public sealed class ErrorController(ILogger<ErrorController> logger) : Controller
{
    [HttpGet]
    public IActionResult Index(int? statusCode)
    {
        var exceptionFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
        var statusFeature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        var requestedStatus = statusCode ?? Response.StatusCode;
        var currentStatus = exceptionFeature is null && requestedStatus is >= 400 and <= 599
            ? requestedStatus
            : StatusCodes.Status500InternalServerError;
        var originalPath = exceptionFeature?.Path ?? statusFeature?.OriginalPath ?? Request.Path.Value ?? string.Empty;

        Response.StatusCode = currentStatus;

        if (exceptionFeature is not null)
        {
            logger.LogError(exceptionFeature.Error, "Unhandled request error for {Path}", originalPath);
        }
        else
        {
            logger.LogWarning("HTTP {StatusCode} for {Path}", currentStatus, originalPath);
        }

        if (originalPath.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            return Problem(
                title: currentStatus == StatusCodes.Status404NotFound
                    ? "Запрошенная операция не найдена."
                    : "Не удалось выполнить запрос. Повторите попытку позже.",
                statusCode: currentStatus);
        }

        var message = currentStatus == StatusCodes.Status404NotFound
            ? "Проверьте адрес страницы или вернитесь на главную."
            : "Попробуйте обновить страницу. Если проблема повторится, мы уже получили уведомление.";
        var model = new ErrorPageViewModel(
            currentStatus == StatusCodes.Status404NotFound
                ? "Страница не найдена"
                : "Что-то пошло не так",
            message,
            HttpContext.TraceIdentifier);

        return View("~/Views/Shared/Error.cshtml", model);
    }
}
