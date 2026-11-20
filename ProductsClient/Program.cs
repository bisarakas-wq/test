
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "Kadr.Admin.Session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.LoginPath = "/admin/login";
        options.AccessDeniedPath = "/admin/login";
        options.SlidingExpiration = false;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }

            context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpClient("ProductsApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ProductsApi:BaseUrl"] ?? "http://localhost:5008/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var app = builder.Build();

app.UseExceptionHandler("/error");
app.UseStatusCodePagesWithReExecute("/error/{0}");
app.Use(async (context, next) =>
{
    if (HttpMethods.IsGet(context.Request.Method))
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.Equals("/booking-solo.html", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/booking-loft.html", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/booking-cafe.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/booking.html", permanent: false);
            return;
        }

        var page = path.ToLowerInvariant() switch
        {
            "/" or "/index.html" => "index",
            "/booking.html" => "booking",
            "/rules.html" => "rules",
            "/policy.html" => "policy",
            "/processing.html" => "processing",
            "/settings.html" => "settings",
            _ => null
        };

        if (page is not null)
        {
            context.Request.Path = $"/site-pages/{page}";
        }
        else if (path.Equals("/studio.html", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Redirect("/", permanent: false);
            return;
        }
    }

    await next();
});
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapControllerRoute(
    name: "order",
    pattern: "order",
    defaults: new { controller = "Products", action = "Order" });

app.MapControllerRoute(
    name: "orders",
    pattern: "orders",
    defaults: new { controller = "Products", action = "Orders" });

app.MapControllerRoute(
    name: "errors",
    pattern: "error/{statusCode?}",
    defaults: new { controller = "Error", action = "Index" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Products}/{action=Index}/{id?}");

app.Run();
