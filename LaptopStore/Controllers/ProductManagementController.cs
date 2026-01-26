using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace LaptopStore.Controllers;

public class ProductManagementController : Controller
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly LaptopStoreDbContext _context = new LaptopStoreDbContext();

    public ProductManagementController(IWebHostEnvironment webHostEnvironment, LaptopStoreDbContext context)
    {
        _webHostEnvironment = webHostEnvironment;
        _context = context;
    }

    public IActionResult Index()
    {
        var products = _context.Products.Include(p => p.Category).Include(p => p.Brand).Include(p => p.ProductImages).ToList();
        var productList = products.Select(p => new ProductResponse
        {
            Id = p.Id,
            Name = p.Name,
            Sku = p.Sku,
            BrandName = p.Brand.Name,
            CategoryName = p.Category.Name,
            Price = p.Price,
            StockQuantity = p.StockQuantity,
            Cpu = p.Cpu,
            Ram = p.Ram,
            HardDrive = p.HardDrive,
            Gpu = p.Gpu,
            ScreenSize = p.ScreenSize,
            Weight = p.Weight,
            ProductImages = p.ProductImages,
            IsActive = p.IsActive
        }).ToList();
        return View("~/Views/Manager/ProductManagement.cshtml", productList);
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

        if (!ModelState.IsValid)
        {

            ViewBag.Brands = _context.Brands.ToList();
            ViewBag.Categories = _context.Categories.ToList();

            return View("~/Views/Manager/AddNewProduct.cshtml", newProduct);
        }

        await _context.Products.AddAsync(new Product
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

        await _context.SaveChangesAsync();

        var product = await _context.Products.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

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

        _context.ProductImages.Add(new ProductImage
        {
            ProductId = product.Id,
            ImageUrl = Path.Combine(folder, fileName),
            IsThumbnail = true
        });

        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult ProductDetails(int id)
    {
        var product = _context.Products
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
