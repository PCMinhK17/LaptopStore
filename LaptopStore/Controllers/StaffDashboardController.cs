using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    [Authorize(Roles = "staff")]
    public class StaffDashboardController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public StaffDashboardController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var last7Days = today.AddDays(-7);

            var stats = new StaffDashboardViewModel
            {
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "pending"),
                ProcessingOrders = await _context.Orders.CountAsync(o => o.Status == "processing"),
                ShippingOrders = await _context.Orders.CountAsync(o => o.Status == "shipping"),
                CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "delivered"),

                // Recent orders
                RecentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                // Low stock products
                LowStockProducts = await _context.Products
                    .Where(p => p.StockQuantity <= 5)
                    .OrderBy(p => p.StockQuantity)
                    .Take(5)
                    .ToListAsync()
            };

            return View("~/Views/Staff/Dashboard.cshtml", stats);
        }

        public IActionResult UnderDevelopment(string feature = "")
        {
            ViewData["Feature"] = feature;
            return View("~/Views/Staff/UnderDevelopment.cshtml");
        }
    }

    public class StaffDashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int CompletedOrders { get; set; }

        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
    }
}
