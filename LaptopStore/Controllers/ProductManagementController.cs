using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;

namespace LaptopStore.Controllers;

[Authorize(Roles = "admin")]
public class ProductManagementController : Controller
{
    private readonly LaptopStoreDbContext _context;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductManagementController(IWebHostEnvironment webHostEnvironment, LaptopStoreDbContext context)
    {
        _context = context;
        _webHostEnvironment = webHostEnvironment;
        _context = context;
    }

    public async Task<IActionResult> Index(int? pageNumber)
    {
        var productsQuery = _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.ProductImages)
            .Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                BrandName = p.Brand.Name,
                CategoryName = p.Category.Name,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ProductImages = p.ProductImages.Select(i => new ProductImageResponse
                {
                    ImageUrl = i.ImageUrl,
                    IsThumbnail = i.IsThumbnail ?? false
                }).ToList(),
                IsActive = p.IsActive
            })
            .OrderByDescending(p => p.Id);

        int pageSize = 10;
        return View("~/Views/Manager/ProductManagement.cshtml", await PaginatedList<ProductResponse>.CreateAsync(productsQuery, pageNumber ?? 1, pageSize));
    }

    // GET: /ProductManagement/ExportExcel
    public async Task<IActionResult> ExportExcel()
    {
        var products = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .OrderByDescending(p => p.Id)
            .ToListAsync();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sản phẩm");

        // Header styling
        var headerRow = worksheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
        headerRow.Style.Font.FontColor = XLColor.White;
        headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        // Headers
        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "SKU";
        worksheet.Cell(1, 3).Value = "Tên sản phẩm";
        worksheet.Cell(1, 4).Value = "Thương hiệu";
        worksheet.Cell(1, 5).Value = "Danh mục";
        worksheet.Cell(1, 6).Value = "Giá";
        worksheet.Cell(1, 7).Value = "Tồn kho";
        worksheet.Cell(1, 8).Value = "CPU";
        worksheet.Cell(1, 9).Value = "RAM";
        worksheet.Cell(1, 10).Value = "Ổ cứng";
        worksheet.Cell(1, 11).Value = "GPU";
        worksheet.Cell(1, 12).Value = "Màn hình";
        worksheet.Cell(1, 13).Value = "Trạng thái";

        // Data rows
        int row = 2;
        foreach (var product in products)
        {
            worksheet.Cell(row, 1).Value = product.Id;
            worksheet.Cell(row, 2).Value = product.Sku ?? "";
            worksheet.Cell(row, 3).Value = product.Name ?? "";
            worksheet.Cell(row, 4).Value = product.Brand?.Name ?? "";
            worksheet.Cell(row, 5).Value = product.Category?.Name ?? "";
            worksheet.Cell(row, 6).Value = product.Price;
            worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(row, 7).Value = product.StockQuantity;
            worksheet.Cell(row, 8).Value = product.Cpu ?? "";
            worksheet.Cell(row, 9).Value = product.Ram ?? "";
            worksheet.Cell(row, 10).Value = product.HardDrive ?? "";
            worksheet.Cell(row, 11).Value = product.Gpu ?? "";
            worksheet.Cell(row, 12).Value = product.ScreenSize ?? "";
            worksheet.Cell(row, 13).Value = product.IsActive == true ? "Đang bán" : "Ngừng bán";

            if (row % 2 == 0)
            {
                worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
            }
            row++;
        }

        worksheet.Columns().AdjustToContents();

        var dataRange = worksheet.Range(1, 1, row - 1, 13);
        dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

        var fileName = $"SanPham_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        return File(stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public IActionResult AddNewProduct()
    {
        ViewBag.Brands = _context.Brands.ToList();
        ViewBag.Categories = _context.Categories.ToList();

        return View("~/Views/Manager/AddNewProduct.cshtml");
    }

    [HttpPost]
    public async Task<IActionResult> AddNewProduct(AddNewProductRequest newProduct)
    {
        if (decimal.TryParse(newProduct.Price, out var price))
        {
            if (price < 1000 || price > 1000000000)
            {
                ModelState.AddModelError("Price", "Giá phải nằm trong khoảng 1000 đến 1000000000");
            }
        }

        if (decimal.TryParse(newProduct.Weight, out var weight))
        {
            if (weight < 1 || weight > 100)
            {
                ModelState.AddModelError("Weight", "Cân nặng phải nằm trong khoảng 1 đến 100");
            }
        }

        if (_context.Products.Select(p => p.Sku).Contains(newProduct.Sku))
        {
            ModelState.AddModelError("Sku", "Mã SKU này đã được dùng bởi sản phẩm khác");
        }

        if (!ModelState.IsValid)
        {

            ViewBag.Brands = _context.Brands.ToList();
            ViewBag.Categories = _context.Categories.ToList();
            TempData["ToastMessage"] = "Vui lòng kiểm tra lại thông tin.";
            TempData["ToastType"] = "error";

            return View("~/Views/Manager/AddNewProduct.cshtml", newProduct);
        }

        var product = new Product
        {
            Name = newProduct.Name,
            Sku = newProduct.Sku,
            BrandId = newProduct.BrandId,
            CategoryId = newProduct.CategoryId,
            Description = newProduct.Description,
            ShortDescription = newProduct.ShortDescription,
            Price = price,
            Cpu = newProduct.Cpu,
            Ram = newProduct.Ram,
            HardDrive = newProduct.HardDrive,
            Gpu = newProduct.Gpu,
            ScreenSize = newProduct.ScreenSize,
            Weight = newProduct.Weight,
        };
        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        //Lưu file ảnh
        if (newProduct.ImageFile != null && newProduct.ImageFile.Length > 0)
        {
            try 
            {
                string folder = "images\\";
                string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);
                
                string fileName = Guid.NewGuid().ToString() + "_" + CleanFileName(newProduct.ImageFile.FileName);
                string filePath = Path.Combine(serverFolder, fileName);

                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await newProduct.ImageFile.CopyToAsync(fileStream);
                }

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = "/" + folder.Replace("\\", "/") + fileName, // Store relative web path
                    IsThumbnail = true
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't stop flow
                Console.WriteLine($"Error saving image: {ex.Message}");
            }
        }

        TempData["ToastMessage"] = "Thêm sản phẩm mới thành công";
        TempData["ToastType"] = "success";

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult ProductDetails(int id)
    {
        var product = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.Id == id);
        if (product == null)
        {
            return NotFound();
        }
        return View("~/Views/Manager/ProductDetails.cshtml", product);
    }

    [HttpGet]
    public IActionResult UpdateProduct(int id)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var product = context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Brands = context.Brands.ToList();
            ViewBag.Categories = context.Categories.ToList();

            var model = new UpdateProductRequest
            {
                Id = product.Id,
                Name = product.Name,
                Sku = product.Sku,
                BrandId = product.BrandId,
                CategoryId = product.CategoryId,
                Price = product.Price.ToString(),
                Description = product.Description,
                ShortDescription = product.ShortDescription,
                Cpu = product.Cpu,
                Ram = product.Ram,
                HardDrive = product.HardDrive,
                Gpu = product.Gpu,
                ScreenSize = product.ScreenSize,
                Weight = product.Weight,
                StockQuantity = product.StockQuantity,
                IsActive = product.IsActive ?? false
            };

            return View("~/Views/Manager/UpdateProduct.cshtml", model);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateProduct(UpdateProductRequest updatedProduct)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            if (decimal.TryParse(updatedProduct.Price, out var price))
            {
                if (price < 1000 || price > 100000000)
                {
                    ModelState.AddModelError("Price", "Giá phải nằm trong khoảng 1000 đến 100000000");
                }
            }
            else
            {
                ModelState.AddModelError("Price", "Giá không hợp lệ");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Brands = context.Brands.ToList();
                ViewBag.Categories = context.Categories.ToList();
                return View("~/Views/Manager/UpdateProduct.cshtml", updatedProduct);
            }

            var product = await context.Products.FirstOrDefaultAsync(p => p.Id == updatedProduct.Id);
            if (product == null)
            {
                return NotFound();
            }

            product.Name = updatedProduct.Name;
            product.Sku = updatedProduct.Sku;
            product.BrandId = updatedProduct.BrandId;
            product.CategoryId = updatedProduct.CategoryId;
            product.Price = price;
            product.Description = updatedProduct.Description;
            product.ShortDescription = updatedProduct.ShortDescription;
            product.Cpu = updatedProduct.Cpu;
            product.Ram = updatedProduct.Ram;
            product.HardDrive = updatedProduct.HardDrive;
            product.Gpu = updatedProduct.Gpu;
            product.ScreenSize = updatedProduct.ScreenSize;
            product.Weight = updatedProduct.Weight;
            product.IsActive = updatedProduct.IsActive;

            if (updatedProduct.ImageFile != null)
            {
                string folder = "images\\";
                string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);

                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + updatedProduct.ImageFile.FileName;
                string filePath = Path.Combine(serverFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await updatedProduct.ImageFile.CopyToAsync(fileStream);
                }

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = "/" + folder.Replace("\\", "/") + fileName,
                    IsThumbnail = true
                });
            }

            await context.SaveChangesAsync();

            return RedirectToAction("ProductDetails", new { id = product.Id });
        }
    }

    [HttpGet]
    public IActionResult ManageProductImages(int productId)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var product = context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            return View("~/Views/Manager/ManageProductImages.cshtml", product);
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadProductImages(int productId, List<IFormFile> imageFiles)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var product = await context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            if (imageFiles != null && imageFiles.Any())
            {
                string folder = "images\\";
                string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);

                if (!Directory.Exists(serverFolder))
                {
                    Directory.CreateDirectory(serverFolder);
                }

                foreach (var file in imageFiles)
                {
                    if (file == null || file.Length == 0) continue;

                    string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(serverFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }

                    var image = new ProductImage
                    {
                        ProductId = product.Id,
                        ImageUrl = "images/" + fileName,
                        IsThumbnail = !product.ProductImages.Any(i => i.IsThumbnail == true)
                    };

                    context.ProductImages.Add(image);
                    product.ProductImages.Add(image);
                }

                await context.SaveChangesAsync();
            }

            return RedirectToAction("ManageProductImages", new { productId = product.Id });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddProductImageUrl(int productId, string imageUrl)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var product = await context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return RedirectToAction("ManageProductImages", new { productId });
            }

            var image = new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = imageUrl.Trim(),
                IsThumbnail = !product.ProductImages.Any(i => i.IsThumbnail == true)
            };

            context.ProductImages.Add(image);
            await context.SaveChangesAsync();

            return RedirectToAction("ManageProductImages", new { productId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> SetMainProductImage(int productId, int imageId)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var images = context.ProductImages.Where(pi => pi.ProductId == productId).ToList();
            if (!images.Any())
            {
                return NotFound();
            }

            foreach (var img in images)
            {
                img.IsThumbnail = img.Id == imageId;
            }

            await context.SaveChangesAsync();

            return RedirectToAction("ManageProductImages", new { productId });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProductImage(int productId, int imageId)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var image = await context.ProductImages.FirstOrDefaultAsync(pi => pi.Id == imageId && pi.ProductId == productId);
            if (image == null)
            {
                return NotFound();
            }

            context.ProductImages.Remove(image);
            await context.SaveChangesAsync();

            return RedirectToAction("ManageProductImages", new { productId });
        }
    }

    private string CleanFileName(string fileName)
    {
        return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c.ToString(), string.Empty));
    }
}

