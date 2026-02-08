using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaptopStore.Models;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    [Authorize(Roles = "admin,staff")]
    public class AdminDashboardController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public AdminDashboardController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var last7Days = today.AddDays(-7);

            // Basic stats
            var stats = new DashboardViewModel
            {
                TotalUsers = await _context.Users.CountAsync(u => u.Role == "customer"),
                TotalProducts = await _context.Products.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "pending"),
                
                // Revenue
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == "delivered")
                    .SumAsync(o => o.TotalMoney),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.Status == "delivered" && o.CreatedAt >= thisMonth)
                    .SumAsync(o => o.TotalMoney),
                TodayRevenue = await _context.Orders
                    .Where(o => o.Status == "delivered" && o.CreatedAt >= today)
                    .SumAsync(o => o.TotalMoney),
                
                // Order counts by status
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
                    .ToListAsync(),
                
                // New users this month
                NewUsersThisMonth = await _context.Users
                    .CountAsync(u => u.Role == "customer" && u.CreatedAt >= thisMonth)
            };

            // Revenue for last 7 days (for chart)
            var revenueByDay = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= last7Days)
                .GroupBy(o => o.CreatedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Revenue = g.Sum(o => o.TotalMoney) })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.ChartLabels = revenueByDay.Select(x => x.Date.ToString("dd/MM")).ToList();
            ViewBag.ChartData = revenueByDay.Select(x => x.Revenue).ToList();

            // Orders for last 7 days
            var ordersByDay = await _context.Orders
                .Where(o => o.CreatedAt >= last7Days)
                .GroupBy(o => o.CreatedAt!.Value.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderBy(x => x.Date)
                .ToListAsync();

            ViewBag.OrderChartLabels = ordersByDay.Select(x => x.Date.ToString("dd/MM")).ToList();
            ViewBag.OrderChartData = ordersByDay.Select(x => x.Count).ToList();

            return View("~/Views/Manager/Dashboard.cshtml", stats);
        }
    }

    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int NewUsersThisMonth { get; set; }
        
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        
        public List<Order> RecentOrders { get; set; } = new();
        public List<Product> LowStockProducts { get; set; } = new();
    }
}
