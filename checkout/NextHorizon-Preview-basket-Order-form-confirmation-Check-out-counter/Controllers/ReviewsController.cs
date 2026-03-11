using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    [ApiController]
    [Route("api/products/{productId}/[controller]")]
    public class ReviewsController : ControllerBase
    {
        // POST: api/products/{productId}/reviews
        [HttpPost]
        public IActionResult AddReview(int productId, [FromBody] Review review)
        {
            var product = ProductData.Products.FirstOrDefault(p => p.Id == productId);
            if (product != null)
            {
                review.Id = product.Reviews.Count + 1;
                review.Date = DateTime.Now;
                review.VerifiedPurchase = true;
                product.Reviews.Add(review);
                
                // Update rating
                product.Rating = product.Reviews.Average(r => r.Rating);
                product.ReviewCount = product.Reviews.Count;
                
                return Ok(new { message = "Review added", product });
            }
            return NotFound();
        }
    }
}
