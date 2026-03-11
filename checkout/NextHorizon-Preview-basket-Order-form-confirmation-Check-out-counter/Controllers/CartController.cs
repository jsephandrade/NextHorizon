using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CartController(AppDbContext db)
        {
            _db = db;
        }

        // POST: api/cart
        [HttpPost]
        public IActionResult AddToCart([FromBody] CartItem item)
        {
            var existing = ProductData.Cart.FirstOrDefault(c => c.ProductId == item.ProductId && c.Size == item.Size);
            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                ProductData.Cart.Add(item);

            return Ok(new { message = "Added to cart", count = ProductData.Cart.Sum(c => c.Quantity) });
        }

        // GET: api/cart
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            if (!ProductData.Cart.Any())
                return Ok(new List<object>());

            var productIds = ProductData.Cart.Select(c => c.ProductId).Distinct().ToList();

            var dbProducts = await _db.Products
                .Where(p => productIds.Contains(p.ProductId))
                .ToListAsync();

            var result = ProductData.Cart.Select(ci =>
            {
                var p = dbProducts.FirstOrDefault(d => d.ProductId == ci.ProductId);
                if (p == null) return null;

                decimal price = p.Discount.HasValue && p.Discount.Value > 0
                    ? p.Price - p.Discount.Value
                    : p.Price;

                return new
                {
                    cartItem = new { ci.ProductId, ci.Size, ci.Quantity },
                    product  = new
                    {
                        id    = p.ProductId,
                        name  = p.ProductName,
                        image = p.ImagePath ?? "",
                        brand = p.Brand ?? "",
                        price
                    }
                };
            })
            .Where(x => x != null)
            .ToList();

            return Ok(result);
        }

        // PUT: api/cart/{productId}
        [HttpPut("{productId}")]
        public IActionResult UpdateQuantity(int productId, [FromBody] UpdateQuantityRequest request)
        {
            var item = ProductData.Cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                if (request.Quantity <= 0)
                {
                    ProductData.Cart.Remove(item);
                    return Ok(new { message = "Removed from cart" });
                }
                item.Quantity = request.Quantity;
                return Ok(new { message = "Quantity updated", quantity = item.Quantity });
            }
            return NotFound();
        }

        // DELETE: api/cart/{productId}
        [HttpDelete("{productId}")]
        public IActionResult RemoveFromCart(int productId)
        {
            var item = ProductData.Cart.FirstOrDefault(c => c.ProductId == productId);
            if (item != null)
            {
                ProductData.Cart.Remove(item);
                return Ok(new { message = "Removed from cart" });
            }
            return NotFound();
        }
    }

    public class UpdateQuantityRequest
    {
        public int Quantity { get; set; }
    }
}
