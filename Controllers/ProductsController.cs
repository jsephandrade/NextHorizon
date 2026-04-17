using Microsoft.AspNetCore.Mvc;
using NextHorizon.Data;
using NextHorizon.Models;

namespace NextHorizon.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        // GET: api/products
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            return Ok(ProductData.Products);
        }

        // GET: api/products/men
        [HttpGet("men")]
        public IActionResult GetMenProducts()
        {
            var menProducts = ProductData.Products.Where(p => p.Category == "Men").ToList();
            return Ok(menProducts);
        }

        // GET: api/products/women
        [HttpGet("women")]
        public IActionResult GetWomenProducts()
        {
            var womenProducts = ProductData.Products.Where(p => p.Category == "Women").ToList();
            return Ok(womenProducts);
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = ProductData.Products.FirstOrDefault(p => p.Id == id);
            return product != null ? Ok(product) : NotFound();
        }

        // GET: api/products/seller/{sellerId}
        [HttpGet("seller/{sellerId}")]
        public IActionResult GetProductsBySeller(int sellerId)
        {
            var products = ProductData.Products.Where(p => p.SellerId == sellerId).ToList();
            return Ok(products);
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class SellersController : ControllerBase
    {
        // GET: api/sellers/{id}
        [HttpGet("{id}")]
        public IActionResult GetSellerById(int id)
        {
            var seller = ProductData.Sellers.FirstOrDefault(s => s.Id == id);
            return seller != null ? Ok(seller) : NotFound();
        }

        // GET: api/sellers
        [HttpGet]
        public IActionResult GetAllSellers()
        {
            return Ok(ProductData.Sellers);
        }
    }
}

