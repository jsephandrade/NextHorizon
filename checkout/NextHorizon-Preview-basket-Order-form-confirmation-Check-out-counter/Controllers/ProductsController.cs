using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ProductsController(AppDbContext db)
        {
            _db = db;
        }

        private Product MapDbProduct(DbProduct p, List<DbProductColorImage>? colorImgs = null)
        {
            var myColorImgs = colorImgs?.Where(ci => ci.ProductId == p.ProductId).ToList()
                              ?? new List<DbProductColorImage>();

            var colorImageDict = myColorImgs
                .GroupBy(ci => ci.ColorName)
                .ToDictionary(g => g.Key, g => g.Select(ci => ci.ImagePath).ToList());

            var availColors = colorImageDict.Keys.ToList();
            if (availColors.Count == 0 && !string.IsNullOrEmpty(p.Colors))
                availColors = p.Colors.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // Build per-color stock dictionary
            var colorStocksList = !string.IsNullOrEmpty(p.ColorStocks)
                ? p.ColorStocks.Split(',').Select(s => int.TryParse(s.Trim(), out var n) ? n : 0).ToList()
                : new List<int>();
            var colorStockDict = new Dictionary<string, int>();
            for (int i = 0; i < availColors.Count; i++)
                colorStockDict[availColors[i]] = i < colorStocksList.Count ? colorStocksList[i] : p.Stock;

            // Build per-color sizes dictionary
            var colorSizesSegments = !string.IsNullOrEmpty(p.ColorSizes)
                ? p.ColorSizes.Split('|')
                : Array.Empty<string>();
            var colorSizesDict = new Dictionary<string, List<string>>();
            for (int i = 0; i < availColors.Count; i++)
            {
                var seg = i < colorSizesSegments.Length ? colorSizesSegments[i] : "";
                if (!string.IsNullOrEmpty(seg))
                    colorSizesDict[availColors[i]] = seg.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }

            return new Product
            {
                Id = p.ProductId,
                Name = p.ProductName,
                Price = p.Discount.HasValue && p.Discount.Value > 0 ? p.Price - p.Discount.Value : p.Price,
                OriginalPrice = p.Discount.HasValue && p.Discount.Value > 0 ? p.Price : null,
                Image = p.ImagePath ?? "",
                Category = p.Category ?? "Unisex",
                SubCategory = p.Category ?? "",
                Brand = p.Brand ?? "",
                Description = p.Details ?? "",
                Rating = 0,
                ReviewCount = 0,
                Stock = p.Stock,
                Sizes = string.IsNullOrEmpty(p.Sizes) ? new List<string>() : p.Sizes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList(),
                AvailableColors = availColors,
                ColorImages = colorImageDict,
                ColorStocks = colorStockDict,
                ColorSizes = colorSizesDict
            };
        }

        // GET: api/products
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var dbProducts = await _db.Products
                .Where(p => p.Status == "active")
                .ToListAsync();

            var productIds = dbProducts.Select(p => p.ProductId).ToList();
            var colorImgs = await _db.ProductColorImages
                .Where(ci => productIds.Contains(ci.ProductId))
                .ToListAsync();

            return Ok(dbProducts.Select(p => MapDbProduct(p, colorImgs)).ToList());
        }

        // GET: api/products/men
        [HttpGet("men")]
        public async Task<IActionResult> GetMenProducts()
        {
            var products = await _db.Products
                .Where(p => p.Status == "active" && p.Category == "Men")
                .ToListAsync();
            var productIds = products.Select(p => p.ProductId).ToList();
            var colorImgs = await _db.ProductColorImages
                .Where(ci => productIds.Contains(ci.ProductId))
                .ToListAsync();
            return Ok(products.Select(p => MapDbProduct(p, colorImgs)).ToList());
        }

        // GET: api/products/women
        [HttpGet("women")]
        public async Task<IActionResult> GetWomenProducts()
        {
            var products = await _db.Products
                .Where(p => p.Status == "active" && p.Category == "Women")
                .ToListAsync();
            var productIds = products.Select(p => p.ProductId).ToList();
            var colorImgs = await _db.ProductColorImages
                .Where(ci => productIds.Contains(ci.ProductId))
                .ToListAsync();
            return Ok(products.Select(p => MapDbProduct(p, colorImgs)).ToList());
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            var dbProduct = await _db.Products.FindAsync(id);
            if (dbProduct != null)
            {
                var colorImgs = await _db.ProductColorImages
                    .Where(ci => ci.ProductId == id)
                    .ToListAsync();
                return Ok(MapDbProduct(dbProduct, colorImgs));
            }
            return NotFound();
        }

        // GET: api/products/seller/{sellerId}
        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetProductsBySeller(int sellerId)
        {
            var products = await _db.Products
                .Where(p => p.Status == "active")
                .ToListAsync();
            var productIds = products.Select(p => p.ProductId).ToList();
            var colorImgs = await _db.ProductColorImages
                .Where(ci => productIds.Contains(ci.ProductId))
                .ToListAsync();
            return Ok(products.Select(p => MapDbProduct(p, colorImgs)).ToList());
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
