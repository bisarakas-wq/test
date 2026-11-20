using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using ProductsClient.Models;

namespace ProductsClient.Controllers;

public sealed class SitePagesController(
    IWebHostEnvironment environment,
    ILogger<SitePagesController> logger) : Controller
{
    private static readonly HashSet<string> PageNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "index",
        "booking",
        "rules",
        "policy",
        "processing",
        "settings"
    };

    private static readonly Regex DocumentPattern = new(
        "\\A\\s*<!doctype\\s+html[^>]*>\\s*<html\\b[^>]*>.*?<head\\b[^>]*>(?<head>.*?)</head>.*?<body\\b[^>]*>(?<body>.*?)</body>.*?</html>\\s*\\z",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex TitlePattern = new("<title\\b[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex CharsetPattern = new("<meta\\b(?=[^>]*\\bcharset\\s*=)[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ViewportPattern = new("<meta\\b(?=[^>]*\\bname\\s*=\\s*[\\\"']viewport[\\\"'])[^>]*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex HeaderPattern = new("<header\\b(?=[^>]*\\bid\\s*=\\s*[\\\"']kadr-header[\\\"'])[^>]*>.*?</header>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex FooterPattern = new("<footer\\b[^>]*>.*?</footer>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    [HttpGet("/site-pages/{page}")]
    public async Task<IActionResult> Index(string page, CancellationToken cancellationToken)
    {
        if (!PageNames.Contains(page))
        {
            return NotFound();
        }

        var path = Path.Combine(environment.ContentRootPath, "SitePageContent", $"{page}.html");
        var html = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
        var document = DocumentPattern.Match(html);
        if (!document.Success)
        {
            logger.LogError("Site page is missing its expected HTML document structure: {Page}", page);
            return Problem("Не удалось загрузить страницу сайта.", statusCode: StatusCodes.Status500InternalServerError);
        }

        var sourceHead = document.Groups["head"].Value;
        var title = TitlePattern.Match(sourceHead);
        var headContent = ViewportPattern.Replace(CharsetPattern.Replace(TitlePattern.Replace(sourceHead, string.Empty), string.Empty), string.Empty);
        var bodyContent = FooterPattern.Replace(HeaderPattern.Replace(document.Groups["body"].Value, string.Empty), string.Empty);
        ViewData["Title"] = title.Success ? WebUtility.HtmlDecode(title.Groups[1].Value.Trim()) : "КАДР";
        var model = new SitePageViewModel(headContent, bodyContent);

        if (page.Equals("booking", StringComparison.OrdinalIgnoreCase))
        {
            return View("~/Views/Booking/Index.cshtml", model);
        }

        ViewData["BaseHref"] = "/";
        return View("~/Views/SitePages/Index.cshtml", model);
    }
}
