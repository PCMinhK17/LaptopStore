using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;

namespace LaptopStore.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly LaptopStoreDbContext _context;
        public ProductController(ILogger<ProductController> logger, LaptopStoreDbContext context)
        {
            _logger = logger;
            _context = context;
        }
        public IActionResult Index()
        {
            //Lấy tất cả product
            var allProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToList();
            //Log số lượng đồ chơi
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            return View("~/Views/Store/Product.cshtml", allProducts);
        }

        public IActionResult FilterByCategory(int categoryId)
        {
            var categories = _context.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.Categories = categories;

            var category = _context.Categories.FirstOrDefault(c => c.Id == categoryId);
            ViewBag.SelectedCategory = category?.Name;

            var products = _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToList();

            return View("Index", products);
        }

        public IActionResult FilterByBrand(int brandId)
        {
            var brands = _context.Brands.OrderBy(b => b.Name).ToList();
            ViewBag.Brands = brands;

            var brand = _context.Brands.FirstOrDefault(b => b.Id == brandId);
            ViewBag.SelectedBrand = brand?.Name;

            var products = _context.Products
                .Where(p => p.BrandId == brandId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToList();

            return View("Index", products);
        }

        public IActionResult Search(string searchTerm)
        {
            searchTerm = searchTerm.Trim();// Loại bỏ khoảng trắng thừa

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction("Index");
            }

            var searchResults = _context.Products
                .Include(p => p.Category != null && p.Category.Name.Contains(searchTerm))
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%") ||
                            EF.Functions.Like(p.Description, $"%{searchTerm}%") ||
                            EF.Functions.Like(p.Category.Name, $"%{searchTerm}%") ||
                            EF.Functions.Like(p.Brand.Name, $"%{searchTerm}%"))
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();


            return View("Index", searchResults);
        }

        public IActionResult FilterByPrice(decimal? minPrice, decimal? maxPrice)
        {
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
            }

            var products = productsQuery.ToList();

            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            return View("Index", products);
        }
    }
}

