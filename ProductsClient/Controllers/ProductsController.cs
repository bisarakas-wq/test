
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ProductsClient.Models;

namespace ProductsClient.Controllers
    {
       public class ProductsController : Controller
        {
            private readonly IHttpClientFactory httpClientFactory;

            public ProductsController(
                IHttpClientFactory httpClientFactory)
            {
                this.httpClientFactory = httpClientFactory;
            }

            public IActionResult Order()
            {
                return View("Orders");
            }

            public IActionResult Orders()
            {
                return View();
            }

            public async Task<IActionResult> Index()
            {
                var products = new List<Product>();

                try
                {
                    var client =
                        httpClientFactory.CreateClient("ProductsApi");

                    products =
                        await client.GetFromJsonAsync<List<Product>>(
                            "api/products")
                        ?? new List<Product>();
                }
                catch (HttpRequestException)
                {
                    ViewBag.Error =
                        "Не удалось получить товары. " +
                        "Проверьте, запущен ли сервер API " +
                        "и правильно ли указан его адрес.";
                }
                catch (TaskCanceledException)
                {
                    ViewBag.Error =
                        "Сервер слишком долго не отвечает. " +
                        "Попробуйте обновить страницу.";
                }
                catch (System.Text.Json.JsonException)
                {
                    ViewBag.Error =
                        "Сервер вернул данные в неожиданном формате.";
                }

                return View(products);
            }
        }
    }

