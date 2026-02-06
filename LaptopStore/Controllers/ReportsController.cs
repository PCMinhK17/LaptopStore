using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;
using System.Globalization;
using ClosedXML.Excel;

namespace LaptopStore.Controllers
{
    [Authorize(Roles = "admin,staff")]
    public class ReportsController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public ReportsController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        // GET: /Reports
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);
            var last30Days = today.AddDays(-30);

            // Total revenue
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "delivered")
                .SumAsync(o => o.TotalMoney);

            // Monthly revenue
            var monthlyRevenue = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= startOfMonth)
                .SumAsync(o => o.TotalMoney);

            // Today's revenue
            var todayRevenue = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= today)
                .SumAsync(o => o.TotalMoney);

            // Order statistics
            var totalOrders = await _context.Orders.CountAsync();
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == "delivered");
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "pending");
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == "cancelled");

            // Average order value
            var avgOrderValue = completedOrders > 0 
                ? totalRevenue / completedOrders 
                : 0;

            // Revenue by month (last 12 months)
            var revenueByMonth = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= startOfYear)
                .GroupBy(o => new { o.CreatedAt!.Value.Year, o.CreatedAt!.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalMoney),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            // Revenue by day (last 30 days)
            var revenueByDay = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= last30Days)
                .GroupBy(o => o.CreatedAt!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalMoney),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Top selling products
            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order!.Status == "delivered")
                .GroupBy(od => new { od.ProductId, od.Product!.Name })
                .Select(g => new
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(10)
                .ToListAsync();

            // Orders by status
            var ordersByStatus = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            // Revenue by payment method
            var revenueByPayment = await _context.Orders
                .Where(o => o.Status == "delivered")
                .GroupBy(o => o.PaymentMethod)
                .Select(g => new
                {
                    Method = g.Key,
                    Revenue = g.Sum(o => o.TotalMoney),
                    Count = g.Count()
                })
                .ToListAsync();

            // Recent orders comparison (this month vs last month)
            var lastMonthStart = startOfMonth.AddMonths(-1);
            var lastMonthRevenue = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= lastMonthStart && o.CreatedAt < startOfMonth)
                .SumAsync(o => o.TotalMoney);

            var revenueGrowth = lastMonthRevenue > 0 
                ? ((monthlyRevenue - lastMonthRevenue) / lastMonthRevenue) * 100 
                : 100;

            ViewBag.TotalRevenue = totalRevenue;
            ViewBag.MonthlyRevenue = monthlyRevenue;
            ViewBag.TodayRevenue = todayRevenue;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.CompletedOrders = completedOrders;
            ViewBag.PendingOrders = pendingOrders;
            ViewBag.CancelledOrders = cancelledOrders;
            ViewBag.AvgOrderValue = avgOrderValue;
            ViewBag.RevenueGrowth = revenueGrowth;

            // Prepare chart data
            ViewBag.MonthlyLabels = revenueByMonth.Select(x => 
                CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(x.Month)).ToList();
            ViewBag.MonthlyRevenues = revenueByMonth.Select(x => x.Revenue).ToList();
            ViewBag.MonthlyOrders = revenueByMonth.Select(x => x.Orders).ToList();

            ViewBag.DailyLabels = revenueByDay.Select(x => x.Date.ToString("dd/MM")).ToList();
            ViewBag.DailyRevenues = revenueByDay.Select(x => x.Revenue).ToList();

            ViewBag.TopProducts = topProducts;
            ViewBag.OrdersByStatus = ordersByStatus;
            ViewBag.RevenueByPayment = revenueByPayment;

            return View("~/Views/Manager/Reports.cshtml");
        }

        // GET: /Reports/ExportExcel
        public async Task<IActionResult> ExportExcel()
        {
            var today = DateTime.Today;
            var startOfYear = new DateTime(today.Year, 1, 1);

            using var workbook = new XLWorkbook();

            // Sheet 1: Revenue Summary
            var summarySheet = workbook.Worksheets.Add("Tổng quan");
            summarySheet.Row(1).Style.Font.Bold = true;
            summarySheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
            summarySheet.Row(1).Style.Font.FontColor = XLColor.White;

            summarySheet.Cell(1, 1).Value = "Chỉ số";
            summarySheet.Cell(1, 2).Value = "Giá trị";

            var totalRevenue = await _context.Orders.Where(o => o.Status == "delivered").SumAsync(o => o.TotalMoney);
            var monthlyRevenue = await _context.Orders.Where(o => o.Status == "delivered" && o.CreatedAt >= new DateTime(today.Year, today.Month, 1)).SumAsync(o => o.TotalMoney);
            var todayRevenue = await _context.Orders.Where(o => o.Status == "delivered" && o.CreatedAt >= today).SumAsync(o => o.TotalMoney);
            var totalOrders = await _context.Orders.CountAsync();
            var completedOrders = await _context.Orders.CountAsync(o => o.Status == "delivered");
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "pending");
            var cancelledOrders = await _context.Orders.CountAsync(o => o.Status == "cancelled");

            summarySheet.Cell(2, 1).Value = "Tổng doanh thu";
            summarySheet.Cell(2, 2).Value = totalRevenue;
            summarySheet.Cell(2, 2).Style.NumberFormat.Format = "#,##0";

            summarySheet.Cell(3, 1).Value = "Doanh thu tháng này";
            summarySheet.Cell(3, 2).Value = monthlyRevenue;
            summarySheet.Cell(3, 2).Style.NumberFormat.Format = "#,##0";

            summarySheet.Cell(4, 1).Value = "Doanh thu hôm nay";
            summarySheet.Cell(4, 2).Value = todayRevenue;
            summarySheet.Cell(4, 2).Style.NumberFormat.Format = "#,##0";

            summarySheet.Cell(5, 1).Value = "Tổng đơn hàng";
            summarySheet.Cell(5, 2).Value = totalOrders;

            summarySheet.Cell(6, 1).Value = "Đơn hoàn thành";
            summarySheet.Cell(6, 2).Value = completedOrders;

            summarySheet.Cell(7, 1).Value = "Đơn chờ xử lý";
            summarySheet.Cell(7, 2).Value = pendingOrders;

            summarySheet.Cell(8, 1).Value = "Đơn đã hủy";
            summarySheet.Cell(8, 2).Value = cancelledOrders;

            summarySheet.Columns().AdjustToContents();

            // Sheet 2: Top Products
            var productSheet = workbook.Worksheets.Add("Top sản phẩm");
            productSheet.Row(1).Style.Font.Bold = true;
            productSheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
            productSheet.Row(1).Style.Font.FontColor = XLColor.White;

            productSheet.Cell(1, 1).Value = "Hạng";
            productSheet.Cell(1, 2).Value = "Tên sản phẩm";
            productSheet.Cell(1, 3).Value = "Số lượng bán";
            productSheet.Cell(1, 4).Value = "Doanh thu";

            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od => od.Order!.Status == "delivered")
                .GroupBy(od => new { od.ProductId, od.Product!.Name })
                .Select(g => new
                {
                    ProductName = g.Key.Name,
                    TotalQuantity = g.Sum(od => od.Quantity),
                    TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(20)
                .ToListAsync();

            int row = 2;
            int rank = 1;
            foreach (var product in topProducts)
            {
                productSheet.Cell(row, 1).Value = rank++;
                productSheet.Cell(row, 2).Value = product.ProductName ?? "";
                productSheet.Cell(row, 3).Value = product.TotalQuantity;
                productSheet.Cell(row, 4).Value = product.TotalRevenue;
                productSheet.Cell(row, 4).Style.NumberFormat.Format = "#,##0";

                if (row % 2 == 0)
                {
                    productSheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                }
                row++;
            }

            productSheet.Columns().AdjustToContents();

            // Sheet 3: Monthly Revenue
            var monthlySheet = workbook.Worksheets.Add("Doanh thu theo tháng");
            monthlySheet.Row(1).Style.Font.Bold = true;
            monthlySheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
            monthlySheet.Row(1).Style.Font.FontColor = XLColor.White;

            monthlySheet.Cell(1, 1).Value = "Tháng";
            monthlySheet.Cell(1, 2).Value = "Số đơn";
            monthlySheet.Cell(1, 3).Value = "Doanh thu";

            var revenueByMonth = await _context.Orders
                .Where(o => o.Status == "delivered" && o.CreatedAt >= startOfYear)
                .GroupBy(o => new { o.CreatedAt!.Value.Year, o.CreatedAt!.Value.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalMoney),
                    Orders = g.Count()
                })
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToListAsync();

            row = 2;
            foreach (var item in revenueByMonth)
            {
                monthlySheet.Cell(row, 1).Value = $"Tháng {item.Month}/{item.Year}";
                monthlySheet.Cell(row, 2).Value = item.Orders;
                monthlySheet.Cell(row, 3).Value = item.Revenue;
                monthlySheet.Cell(row, 3).Style.NumberFormat.Format = "#,##0";

                if (row % 2 == 0)
                {
                    monthlySheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                }
                row++;
            }

            monthlySheet.Columns().AdjustToContents();

            var fileName = $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        // API for dashboard charts
        [HttpGet]
        public async Task<IActionResult> GetRevenueData(string period = "month")
        {
            var today = DateTime.Today;
            
            if (period == "week")
            {
                var startDate = today.AddDays(-7);
                var data = await _context.Orders
                    .Where(o => o.Status == "delivered" && o.CreatedAt >= startDate)
                    .GroupBy(o => o.CreatedAt!.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("dd/MM"),
                        Revenue = g.Sum(o => o.TotalMoney),
                        Orders = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();
                return Json(data);
            }
            else if (period == "month")
            {
                var startDate = today.AddDays(-30);
                var data = await _context.Orders
                    .Where(o => o.Status == "delivered" && o.CreatedAt >= startDate)
                    .GroupBy(o => o.CreatedAt!.Value.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("dd/MM"),
                        Revenue = g.Sum(o => o.TotalMoney),
                        Orders = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();
                return Json(data);
            }
            else // year
            {
                var startOfYear = new DateTime(today.Year, 1, 1);
                var data = await _context.Orders
                    .Where(o => o.Status == "delivered" && o.CreatedAt >= startOfYear)
                    .GroupBy(o => new { o.CreatedAt!.Value.Year, o.CreatedAt!.Value.Month })
                    .Select(g => new
                    {
                        Date = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(g.Key.Month),
                        Revenue = g.Sum(o => o.TotalMoney),
                        Orders = g.Count()
                    })
                    .ToListAsync();
                return Json(data);
            }
        }
    }
}

