using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers;

public class ProductManagementController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProductManagementController(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public IActionResult Index()
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var products = context.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.ProductImages).ToList();
            var productList = products.Select(p => new ProductResponse
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                BrandName = p.Brand.Name,
                CategoryName = p.Category.Name,
                Price = p.Price,
                StockQuantity = p.StockQuantity,
                ProductImages = p.ProductImages,
                IsActive = p.IsActive
            }).ToList();
            return View("~/Views/Manager/ProductManagement.cshtml", productList);
        }
    }

    [HttpGet]
    public IActionResult AddNewProduct()
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            ViewBag.Brands = context.Brands.ToList();
            ViewBag.Categories = context.Categories.ToList();

            return View("~/Views/Manager/AddNewProduct.cshtml");
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddNewProduct(AddNewProductRequest newProduct)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            if (decimal.TryParse(newProduct.Price, out var price))
            {
                if (price < 1000 || price > 100000000)
                {
                    ModelState.AddModelError("Price", "Giá phải nằm trong khoảng 1000 đến 100000000");
                }
            }

            if (!ModelState.IsValid)
            {

                ViewBag.Brands = context.Brands.ToList();
                ViewBag.Categories = context.Categories.ToList();

                return View("~/Views/Manager/AddNewProduct.cshtml", newProduct);
            }

            await context.Products.AddAsync(new Product
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
                Weight = newProduct.Weight
            });

            await context.SaveChangesAsync();

            var product = await context.Products.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

            //Lưu file ảnh
            string folder = "images\\";
            string serverFolder = Path.Combine(_webHostEnvironment.WebRootPath, folder);
            Console.WriteLine(serverFolder);

            string fileName = Guid.NewGuid().ToString() + "_" + newProduct.ImageFile?.FileName;
            string filePath = Path.Combine(serverFolder, fileName);

            Console.WriteLine(filePath);

            if (!Directory.Exists(serverFolder))
            {
                Directory.CreateDirectory(serverFolder);
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await newProduct.ImageFile.CopyToAsync(fileStream);
            }

            context.ProductImages.Add(new ProductImage
            {
                ProductId = product.Id,
                ImageUrl = "images/" + fileName, // Dùng forward slash cho URL web
                IsThumbnail = true
            });

            await context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }

    [HttpGet]
    public IActionResult ProductDetails(int id)
    {
        using (LaptopStoreDbContext context = new LaptopStoreDbContext())
        {
            var product = context.Products
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

                context.ProductImages.Add(new ProductImage
                {
                    ProductId = product.Id,
                    ImageUrl = "images/" + fileName,
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
}
