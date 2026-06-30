using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Data;

namespace MyAspNetApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BrandsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetBrands()
    {
        var brands = ProductData.Products
            .Where(product => !string.IsNullOrWhiteSpace(product.Brand))
            .GroupBy(product => product.Brand)
            .Select(group => new
            {
                name = group.Key,
                count = group.Count()
            })
            .OrderByDescending(brand => brand.count)
            .ThenBy(brand => brand.name)
            .ToList();

        return Ok(brands);
    }
}
