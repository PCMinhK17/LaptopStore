using System.Diagnostics;
using LaptopStore.Models;
using LaptopStore.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LaptopStoreDbContext _context;

        public HomeController(ILogger<HomeController> logger, LaptopStoreDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            // Featured products - 8 newest active products
            var featuredProducts = _context.Products
                .Where(p => p.IsActive == true)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.Reviews)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToList();

            // All brands
            var brands = _context.Brands.ToList();

            // Categories with product count
            var categories = _context.Categories
                .Where(c => c.IsActive == true)
                .Select(c => new {
                    Category = c,
                    ProductCount = c.Products.Count(p => p.IsActive == true)
                })
                .ToList();

            // User-specific data
            int? userId = User.GetUserId();
            var wishlistProductIds = new List<int?>();
            int cartItemCount = 0;
            int wishlistCount = 0;

            if (userId.HasValue)
            {
                wishlistProductIds = _context.WishlistItems
                    .Where(wi => wi.Wishlist != null && wi.Wishlist.UserId == userId)
                    .Select(wi => wi.ProductId)
                    .ToList();

                cartItemCount = _context.CartItems
                    .Count(ci => ci.Cart != null && ci.Cart.UserId == userId);

                wishlistCount = wishlistProductIds.Count;
            }

            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.Brands = brands;
            ViewBag.Categories = categories.Select(c => c.Category).ToList();
            ViewBag.CategoryProductCounts = categories.ToDictionary(c => c.Category.Id, c => c.ProductCount);
            ViewBag.WishlistProductIds = wishlistProductIds;
            ViewBag.CartItemCount = cartItemCount;
            ViewBag.WishlistCount = wishlistCount;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
