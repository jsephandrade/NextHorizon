using Microsoft.AspNetCore.Mvc;
using WebApplication1.Data;
using WebApplication1.Models;
using WebApplication1.ViewModels;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        // POST: api/cart
        [HttpPost]
        public IActionResult AddToCart([FromBody] CartItem item)
        {
            ProductData.Cart.Add(item);
            return Ok(new { message = "Added to cart", cart = ProductData.Cart });
        }

        // GET: api/cart
        [HttpGet]
        // GET: api/cart
[HttpGet]
public IActionResult GetCart()
{
    var cartWithProducts = ProductData.Cart
        .Select(ci => new
        {
            CartItem = ci,
            Product = ProductData.Product.FirstOrDefault(p => p.Id == ci.ProductId) 
        })
        .Where(x => x.Product != null)
        .ToList();
    return Ok(cartWithProducts);
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