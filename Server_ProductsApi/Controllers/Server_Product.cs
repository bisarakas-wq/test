using Microsoft.AspNetCore.Mvc;

namespace Server_ProductsApi.Controllers
{
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = new[]
            {
                new
                {
                    Id = 1,
                    Name = "Тетрадь",
                    Price = 250,
                    Quantity = 40
                },
                new
                {
                    Id = 2,
                    Name = "Ручка",
                    Price = 150,
                    Quantity = 60
                }
            };

            return Ok(products);
        }
    }
}
