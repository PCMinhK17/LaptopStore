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
        public IActionResult Index(int page = 1)
        {
            int pageSize = 9;
            //Lấy tất cả product
            var allProducts = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToList();
            //Lấy tổng số lượng sản phẩm
            int totalProducts = allProducts.Count;
            var products = allProducts.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            //Log số lượng đồ chơi
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            return View("~/Views/Store/Product.cshtml", products);
        }

        public IActionResult SearchByCategoryAndBrand(int? brandId, int? categoryId, int? priceRange)
        {
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();

            ViewBag.SelectedBrandId = brandId;
            ViewBag.SelectedCategoryId = categoryId;
            ViewBag.SelectedPriceRange = priceRange;


            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .AsQueryable();

            if (brandId.HasValue)
                productsQuery = productsQuery.Where(p => p.BrandId == brandId.Value);

            if (categoryId.HasValue)
                productsQuery = productsQuery.Where(p => p.CategoryId == categoryId.Value);
            if (priceRange.HasValue)
            {
                switch (priceRange.Value)
                {
                    case 1:
                        productsQuery = productsQuery.Where(p => p.Price < 10000000);
                        break;
                    case 2:
                        productsQuery = productsQuery.Where(p => p.Price >= 10000000 && p.Price <= 20000000);
                        break;
                    case 3:
                        productsQuery = productsQuery.Where(p => p.Price > 20000000 && p.Price <= 50000000);
                        break;
                    case 4:
                        productsQuery = productsQuery.Where(p => p.Price > 50000000);
                        break;
                }
            }

            var products = productsQuery.ToList();

            return View("~/Views/Store/Product.cshtml", products);
        }

        public IActionResult FilterByCategory(int categoryId)
        {
            ViewBag.Brands = _context.Brands.OrderBy(b => b.Name).ToList();
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();

            var category = _context.Categories.FirstOrDefault(c => c.Id == categoryId);
            ViewBag.SelectedCategory = category?.Name;

            var products = _context.Products
                .Where(p => p.CategoryId == categoryId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToList();

            return View("~/Views/Store/Product.cshtml", products);
        }

        public IActionResult FilterByBrand(int brandId)
        {
            ViewBag.Categories = _context.Categories.OrderBy(c => c.Name).ToList();
            ViewBag.Brands = _context.Brands.OrderBy(b => b.Name).ToList();

            var brand = _context.Brands.FirstOrDefault(b => b.Id == brandId);
            ViewBag.SelectedBrand = brand?.Name;

            var products = _context.Products
                .Where(p => p.BrandId == brandId)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .ToList();

            return View("~/Views/Store/Product.cshtml", products);
        }

        public IActionResult Search(string searchTerm)
        {
            searchTerm = searchTerm?.Trim();// Loại bỏ khoảng trắng thừa

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction("Index");
            }

            var searchResults = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Where(p => EF.Functions.Like(p.Name, $"%{searchTerm}%") ||
                            EF.Functions.Like(p.Description, $"%{searchTerm}%") ||
                            (p.Category != null && EF.Functions.Like(p.Category.Name, $"%{searchTerm}%")) ||
                            EF.Functions.Like(p.Brand.Name, $"%{searchTerm}%"))
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.Brands = _context.Brands.ToList();


            return View("~/Views/Store/Product.cshtml", searchResults);
        }
        public IActionResult Detail(int id, int page = 1)
        {
            int pageSize = 10;

            var productDetail = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.Id == id);

            if (productDetail == null)
            {
                return NotFound();
            }

            // Lấy toàn bộ review để tính thống kê rating
            var reviewsList = _context.Reviews
                .Where(r => r.ProductId == id)
                .ToList();

            int totalReviews = reviewsList.Count;

            // Query review để phân trang
            var reviewsQuery = _context.Reviews
                .Where(r => r.ProductId == id)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt);

            var reviews = reviewsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Reviews = reviews;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

            // Tính rating trung bình
            var avgRating = _context.Reviews
                .Where(r => r.ProductId == id)
                .Average(r => (double?)r.Rating) ?? 0;

            ViewBag.AvgRating = Math.Round(avgRating, 1);
            ViewBag.TotalReviews = totalReviews;

            // Thống kê số lượng từng mức sao
            ViewBag.Rating5 = reviewsList.Count(r => r.Rating == 5);
            ViewBag.Rating4 = reviewsList.Count(r => r.Rating == 4);
            ViewBag.Rating3 = reviewsList.Count(r => r.Rating == 3);
            ViewBag.Rating2 = reviewsList.Count(r => r.Rating == 2);
            ViewBag.Rating1 = reviewsList.Count(r => r.Rating == 1);

            return View("~/Views/Store/Detail.cshtml", productDetail);
        }
    }
}

