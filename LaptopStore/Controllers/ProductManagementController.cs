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
                ImageUrl = Path.Combine(folder, fileName),
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
                .FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/Manager/ProductDetails.cshtml", product);
        }
    }
}
