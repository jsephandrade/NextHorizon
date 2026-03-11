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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Data.SqlClient;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
       
        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public ProductsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _db = context;
            _cache = cache;
        }

        private static Review MapDbReview(DbReview review, List<DbReviewImage> reviewImages)
        {
            return new Review
            {
                Id = review.Id,
                UserName = review.UserName,
                Rating = (double)review.Rating,
                ShortReview = review.ShortReview,
                Comment = review.Comment,
                Date = review.Date,
                VerifiedPurchase = review.VerifiedPurchase,
                Email = review.Email,
                Recommend = review.Recommend,
                Comfort = review.Comfort,
                Quality = review.Quality,
                SizeFit = review.SizeFit,
                WidthFit = review.WidthFit,
                SellerReply = review.SellerReply,
                SellerReplyDate = review.SellerReplyDate,
                ProductId = review.ProductId,
                Images = reviewImages
                    .Where(i => i.ReviewId == review.Id)
                    .Select(i => i.ImageUrl)
                    .ToList()
            };
        }

        private Product MapDbProduct(
            DbProduct p,
            List<DbProductColorImage>? colorImgs = null,
            Dictionary<int, (double Rating, int Count)>? reviewStats = null,
            Dictionary<int, List<Review>>? reviewsByProduct = null)
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

            var rating = 0d;
            var reviewCount = 0;
            if (reviewStats != null && reviewStats.TryGetValue(p.ProductId, out var stats))
            {
                rating = stats.Rating;
                reviewCount = stats.Count;
            }

            var mappedReviews = new List<Review>();
            if (reviewsByProduct != null && reviewsByProduct.TryGetValue(p.ProductId, out var fullReviews))
                mappedReviews = fullReviews;

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
                Rating = rating,
                ReviewCount = reviewCount,
                Reviews = mappedReviews,
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
            try
            {
                var result = await _cache.GetOrCreateAsync("products:all", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);

                    var dbProducts = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Status == "active")
                        .ToListAsync();

                    var productIds = dbProducts.Select(p => p.ProductId).ToList();
                    var colorImgs = await _db.ProductColorImages
                        .AsNoTracking()
                        .Where(ci => productIds.Contains(ci.ProductId))
                        .ToListAsync();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync();
                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));

                    return dbProducts.Select(p => MapDbProduct(p, colorImgs, reviewStats)).ToList();
                });

                if (result == null)
                {
                    return Ok(new List<Product>());
                }

                return Ok(result);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/men
        [HttpGet("men")]
        public async Task<IActionResult> GetMenProducts()
        {
            try
            {
                var result = await _cache.GetOrCreateAsync("products:men", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);

                    var products = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Status == "active" && p.Category == "Men")
                        .ToListAsync();
                    var productIds = products.Select(p => p.ProductId).ToList();
                    var colorImgs = await _db.ProductColorImages
                        .AsNoTracking()
                        .Where(ci => productIds.Contains(ci.ProductId))
                        .ToListAsync();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync();
                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));
                    return products.Select(p => MapDbProduct(p, colorImgs, reviewStats)).ToList();
                });

                if (result == null)
                {
                    return Ok(new List<Product>());
                }

                return Ok(result);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/women
        [HttpGet("women")]
        public async Task<IActionResult> GetWomenProducts()
        {
            try
            {
                var result = await _cache.GetOrCreateAsync("products:women", async entry =>
                {
                    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(20);

                    var products = await _db.Products
                        .AsNoTracking()
                        .Where(p => p.Status == "active" && p.Category == "Women")
                        .ToListAsync();
                    var productIds = products.Select(p => p.ProductId).ToList();
                    var colorImgs = await _db.ProductColorImages
                        .AsNoTracking()
                        .Where(ci => productIds.Contains(ci.ProductId))
                        .ToListAsync();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => productIds.Contains(r.ProductId))
                        .ToListAsync();
                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));
                    return products.Select(p => MapDbProduct(p, colorImgs, reviewStats)).ToList();
                });

                if (result == null)
                {
                    return Ok(new List<Product>());
                }

                return Ok(result);
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var dbProduct = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == id);
                if (dbProduct != null)
                {
                    var colorImgs = await _db.ProductColorImages
                        .AsNoTracking()
                        .Where(ci => ci.ProductId == id)
                        .ToListAsync();
                    var dbReviews = await _db.Reviews
                        .AsNoTracking()
                        .Where(r => r.ProductId == id)
                        .OrderByDescending(r => r.Date)
                        .ToListAsync();
                    var reviewImages = await _db.ReviewImages
                        .AsNoTracking()
                        .Where(i => dbReviews.Select(r => r.Id).Contains(i.ReviewId))
                        .ToListAsync();

                    var mappedReviews = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(r => MapDbReview(r, reviewImages)).ToList());
                    var reviewStats = dbReviews
                        .GroupBy(r => r.ProductId)
                        .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));

                    return Ok(MapDbProduct(dbProduct, colorImgs, reviewStats, mappedReviews));
                }
                return NotFound();
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        // GET: api/products/seller/{sellerId}
        [HttpGet("seller/{sellerId}")]
        public async Task<IActionResult> GetProductsBySeller(int sellerId)
        {
            try
            {
                var products = await _db.Products
                    .AsNoTracking()
                    .Where(p => p.Status == "active")
                    .ToListAsync();
                var productIds = products.Select(p => p.ProductId).ToList();
                var colorImgs = await _db.ProductColorImages
                    .AsNoTracking()
                    .Where(ci => productIds.Contains(ci.ProductId))
                    .ToListAsync();
                var dbReviews = await _db.Reviews
                    .AsNoTracking()
                    .Where(r => productIds.Contains(r.ProductId))
                    .ToListAsync();
                var reviewStats = dbReviews
                    .GroupBy(r => r.ProductId)
                    .ToDictionary(g => g.Key, g => ((double)g.Average(x => x.Rating), g.Count()));
                return Ok(products.Select(p => MapDbProduct(p, colorImgs, reviewStats)).ToList());
            }
            catch (Exception ex) when (IsTransientDatabaseException(ex))
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Database is temporarily unavailable." });
            }
        }

        private static bool IsTransientDatabaseException(Exception ex)
        {
            if (ex is TimeoutException || ex is SqlException)
            {
                return true;
            }

            if (ex is InvalidOperationException ioe &&
                ioe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (ex.InnerException is TimeoutException || ex.InnerException is SqlException)
            {
                return true;
            }

            return ex.InnerException is InvalidOperationException innerIoe &&
                   innerIoe.Message.Contains("connection from the pool", StringComparison.OrdinalIgnoreCase);
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
        var seller = ProductData.Seller.FirstOrDefault(s => s.Id == id);
        return seller != null ? Ok(seller) : NotFound();
    }

    // GET: api/sellers
    [HttpGet]
    public IActionResult GetAllSellers()
    {
        return Ok(ProductData.Seller);
    }
}
}