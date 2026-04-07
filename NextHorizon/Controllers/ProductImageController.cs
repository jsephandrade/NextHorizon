using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextHorizon.Data;

namespace NextHorizon.Controllers;

public class ProductImageController : Controller
{
    private readonly AppDbContext _db;

    public ProductImageController(AppDbContext db)
    {
        _db = db;
    }

    // Serves binary image for a variant: <img src="/ProductImage/Variant/42" />
    [HttpGet("/ProductImage/Variant/{variantId:int}")]
    public async Task<IActionResult> Variant(int variantId)
    {
        var variant = await _db.ProductVariants
            .AsNoTracking()
            .Where(v => v.Id == variantId)
            .Select(v => new { v.ImageData, v.ImageMimeType, v.ImagePath })
            .FirstOrDefaultAsync();

        if (variant == null) return NotFound();

        // Serve binary data if available
        if (variant.ImageData != null && variant.ImageData.Length > 0)
        {
            var mime = string.IsNullOrEmpty(variant.ImageMimeType) ? "image/jpeg" : variant.ImageMimeType;
            return File(variant.ImageData, mime);
        }

        // Fallback: redirect to file path if binary not yet migrated
        if (!string.IsNullOrEmpty(variant.ImagePath))
            return Redirect(variant.ImagePath);

        return NotFound();
    }

    // ONE-TIME MIGRATION: Converts existing file-path images to VARBINARY for a seller's variants
    // Call once via: GET /ProductImage/MigrateSellerImages/5
    // Remove or restrict this endpoint after migration is complete
    [HttpGet("/ProductImage/MigrateSellerImages/{sellerId:int}")]
    public async Task<IActionResult> MigrateSellerImages(int sellerId)
    {
        var variants = await _db.ProductVariants
            .Include(v => v.Product)
            .Where(v => v.Product!.SellerId == sellerId
                && v.ImageData == null
                && !string.IsNullOrEmpty(v.ImagePath))
            .ToListAsync();

        int converted = 0, skipped = 0;
        var results = new List<string>();

        foreach (var variant in variants)
        {
            var relativePath = variant.ImagePath.TrimStart('/');
            var physicalPath = Path.Combine(
                Directory.GetCurrentDirectory(), "wwwroot",
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(physicalPath))
            {
                results.Add($"VariantId {variant.Id}: SKIPPED - file not found");
                skipped++;
                continue;
            }

            var bytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            var ext = Path.GetExtension(physicalPath).ToLowerInvariant();
            variant.ImageData = bytes;
            variant.ImageMimeType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png"            => "image/png",
                ".gif"            => "image/gif",
                ".webp"           => "image/webp",
                _                 => "image/jpeg"
            };

            results.Add($"VariantId {variant.Id}: OK - {bytes.Length} bytes ({variant.ImageMimeType})");
            converted++;
        }

        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = $"Migration complete. Converted: {converted}, Skipped: {skipped}",
            details = results
        });
    }
}
