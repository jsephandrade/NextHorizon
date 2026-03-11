using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    public class SellerController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public SellerController(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // GET: /Seller - Seller Dashboard (loads products from DB)
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products.ToListAsync();
            return View(products);
        }

        // GET: /Seller/CreateProduct
        public async Task<IActionResult> CreateProduct(string? mode, int? id)
        {
            ViewBag.Mode = mode ?? "create";
            ViewBag.ExistingColorImages = "{}";
            if (id.HasValue && (mode == "edit" || mode == "renew" || mode == "relist"))
            {
                var product = await _db.Products.FindAsync(id.Value);
                if (product != null)
                {
                    var colorImages = await _db.ProductColorImages
                        .Where(ci => ci.ProductId == product.ProductId)
                        .ToListAsync();
                    var grouped = colorImages
                        .GroupBy(ci => ci.ColorName)
                        .ToDictionary(g => g.Key, g => g.Select(ci => ci.ImagePath).ToList());
                    ViewBag.ExistingColorImages = System.Text.Json.JsonSerializer.Serialize(grouped);
                    return View(product);
                }
            }
            return View(new DbProduct());
        }

        // POST: /Seller/CreateProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(DbProduct model, IFormFile? imageFile, string? mode, string[]? colorNames, string[]? colorStocks, string[]? colorSizes)
        {
            ModelState.Remove("imageFile");
            ModelState.Remove("mode");
            ModelState.Remove("colorNames");
            ModelState.Remove("colorStocks");
            ModelState.Remove("colorSizes");

            // Fallback when browser sends multiple files and default binding doesn't populate imageFile.
            if ((imageFile == null || imageFile.Length == 0) && Request.Form.Files.Count > 0)
            {
                imageFile = Request.Form.Files.FirstOrDefault(f => f.Name == "imageFile")
                            ?? Request.Form.Files.FirstOrDefault();
            }

            // Handle main product image
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsDir);
                var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(imageFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);
                using var stream = new FileStream(filePath, FileMode.Create);
                await imageFile.CopyToAsync(stream);
                model.ImagePath = "/uploads/products/" + fileName;
            }

            // Derive Colors column from color entries
            var colorNameList = (colorNames ?? Array.Empty<string>())
                .Where(n => !string.IsNullOrWhiteSpace(n)).ToList();
            model.Colors = colorNameList.Count > 0
                ? string.Join(",", colorNameList)
                : model.Colors;

            var colorStockList = (colorStocks ?? Array.Empty<string>())
                .Select(s => int.TryParse(s, out var n) ? n : 0).ToList();
            model.ColorStocks = colorStockList.Count > 0
                ? string.Join(",", colorStockList)
                : model.ColorStocks;

            var colorSizeList = (colorSizes ?? Array.Empty<string>())
                .Select(s => s?.Trim() ?? "").ToList();
            model.ColorSizes = colorSizeList.Count > 0
                ? string.Join("|", colorSizeList)
                : model.ColorSizes;

            int productId;

            if (mode == "edit" && model.ProductId > 0)
            {
                var existing = await _db.Products.FindAsync(model.ProductId);
                if (existing != null)
                {
                    existing.ProductName = model.ProductName;
                    existing.Price = model.Price;
                    existing.Discount = model.Discount;
                    existing.Category = model.Category;
                    existing.Details = model.Details;
                    existing.Brand = model.Brand;
                    existing.Stock = model.Stock;
                    existing.Colors = model.Colors;
                    existing.Sizes = model.Sizes;
                    existing.ColorStocks = model.ColorStocks;
                    existing.ColorSizes = model.ColorSizes;
                    existing.Status = model.Status ?? "active";
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        existing.ImagePath = model.ImagePath;
                    await _db.SaveChangesAsync();
                }
                productId = model.ProductId;
            }
            else if (mode == "relist" && model.ProductId > 0)
            {
                var existing = await _db.Products.FindAsync(model.ProductId);
                if (existing != null)
                {
                    existing.Status = "active";
                    existing.ProductName = model.ProductName;
                    existing.Price = model.Price;
                    existing.Discount = model.Discount;
                    existing.Category = model.Category;
                    existing.Details = model.Details;
                    existing.Brand = model.Brand;
                    existing.Stock = model.Stock;
                    existing.Colors = model.Colors;
                    existing.Sizes = model.Sizes;
                    existing.ColorStocks = model.ColorStocks;
                    existing.ColorSizes = model.ColorSizes;
                    if (!string.IsNullOrEmpty(model.ImagePath))
                        existing.ImagePath = model.ImagePath;
                    await _db.SaveChangesAsync();
                }
                productId = model.ProductId;
            }
            else
            {
                model.Status = model.Status ?? "active";
                _db.Products.Add(model);
                await _db.SaveChangesAsync();
                productId = model.ProductId;
            }

            // Save color images
            if (colorNameList.Count > 0)
            {
                // Remove old color images for this product
                var oldImages = _db.ProductColorImages.Where(ci => ci.ProductId == productId);
                _db.ProductColorImages.RemoveRange(oldImages);
                await _db.SaveChangesAsync();

                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "products");
                Directory.CreateDirectory(uploadsDir);

                for (int i = 0; i < colorNameList.Count; i++)
                {
                    var colorName = colorNameList[i].Trim();

                    // Re-insert existing images that were kept
                    var existingPaths = Request.Form[$"existingColorPaths_{i}"].ToString();
                    if (!string.IsNullOrEmpty(existingPaths))
                    {
                        foreach (var path in existingPaths.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            _db.ProductColorImages.Add(new DbProductColorImage
                            {
                                ProductId = productId,
                                ColorName = colorName,
                                ImagePath = path
                            });
                        }
                    }

                    // Save newly uploaded files for this color
                    var colorFiles = Request.Form.Files.GetFiles($"colorFiles_{i}");
                    foreach (var file in colorFiles)
                    {
                        if (file.Length > 0)
                        {
                            var fn = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName);
                            var fp = Path.Combine(uploadsDir, fn);
                            using var fs = new FileStream(fp, FileMode.Create);
                            await file.CopyToAsync(fs);
                            _db.ProductColorImages.Add(new DbProductColorImage
                            {
                                ProductId = productId,
                                ColorName = colorName,
                                ImagePath = "/uploads/products/" + fn
                            });
                        }
                    }
                }
                await _db.SaveChangesAsync();

                // Set product main image from first color's first photo if not already set
                if (string.IsNullOrEmpty(model.ImagePath))
                {
                    var firstColorImage = await _db.ProductColorImages
                        .Where(ci => ci.ProductId == productId)
                        .OrderBy(ci => ci.Id)
                        .FirstOrDefaultAsync();
                    if (firstColorImage != null)
                    {
                        var product = await _db.Products.FindAsync(productId);
                        if (product != null)
                        {
                            product.ImagePath = firstColorImage.ImagePath;
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }

            if (mode == "edit")
                TempData["SuccessMessage"] = "Product updated successfully.";
            else if (mode == "relist")
                TempData["SuccessMessage"] = "Product relisted successfully.";
            else
                TempData["SuccessMessage"] = "Product added successfully.";

            return RedirectToAction("Index");
        }

        // GET: /Seller/ViewProduct?id=1
        public async Task<IActionResult> ViewProduct(int? id, string? state)
        {
            ViewBag.State = state ?? "active";
            if (id.HasValue)
            {
                var product = await _db.Products.FindAsync(id.Value);
                if (product != null)
                {
                    var colorImages = await _db.ProductColorImages
                        .Where(ci => ci.ProductId == id.Value)
                        .Select(ci => ci.ImagePath)
                        .Distinct()
                        .ToListAsync();
                    ViewBag.AllColorImages = colorImages;
                    return View(product);
                }
            }
            return View(new DbProduct());
        }

        // POST: /Seller/DeleteProduct
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = "relist";
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Product removed.";
            }
            return RedirectToAction("Index");
        }

        // POST: /Seller/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var product = await _db.Products.FindAsync(id);
            if (product != null)
            {
                product.Status = status;
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "Status updated.";
            }
            return RedirectToAction("Index");
        }

        // GET: /Seller/SizeGuide
        public IActionResult SizeGuide()
        {
            return View();
        }

        // GET: /Seller/ViewSizeGuide?id=1
        [HttpGet]
        public async Task<IActionResult> ViewSizeGuide(int id)
        {
            var product = await _db.Products.FindAsync(id);
            var model = new MyAspNetApp.Models.ViewSizeGuideViewModel
            {
                ProductId = id,
                ProductTitle = product?.ProductName ?? "Product Size Guide",
                IsPhotoUpload = false,
                MeasurementUnit = "in",
                Category = product?.Category ?? "Tops",
                TableTitle = "Size Chart",
                FitTips = "If you're on the borderline between two sizes, order the smaller size for a tighter fit or the larger size for a looser fit.",
                HowToMeasure = "Chest: Measure around the fullest part of your chest, keeping the measuring tape horizontal.",
                TableData = new List<List<string>>
                {
                    new List<string> { "Size", "XXS", "XS", "S", "M", "L", "XL", "XXL" },
                    new List<string> { "Chest (in.)", "28.5–30", "30–32", "32–33.5", "33.5–35", "35–37.5", "37.5–40", "40–42.5" },
                    new List<string> { "Waist (in.)", "24.5–26", "26–27", "27–28", "28–29.5", "29.5–31.5", "31.5–33.5", "33.5–35.5" },
                    new List<string> { "Hip (in.)", "33–34", "34–35", "35–36.5", "36.5–38", "38–40", "40–42", "42–44" }
                }
            };
            return View(model);
        }
    }
}
