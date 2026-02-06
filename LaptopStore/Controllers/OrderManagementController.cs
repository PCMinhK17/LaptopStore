using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;
using ClosedXML.Excel;

namespace LaptopStore.Controllers
{
    [Authorize(Roles = "admin,staff")]
    public class OrderManagementController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public OrderManagementController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        // GET: /OrderManagement
        public async Task<IActionResult> Index(string status, string search, int? pageNumber)
        {
            int pageSize = 10;
            
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            // Search by order ID, customer name, or phone
            if (!string.IsNullOrEmpty(search))
            {
                ordersQuery = ordersQuery.Where(o => 
                    o.Id.ToString().Contains(search) ||
                    o.FullName.Contains(search) ||
                    o.PhoneNumber.Contains(search));
            }

            // Get counts for each status
            ViewBag.AllCount = await _context.Orders.CountAsync();
            ViewBag.PendingCount = await _context.Orders.CountAsync(o => o.Status == "pending");
            ViewBag.ProcessingCount = await _context.Orders.CountAsync(o => o.Status == "processing");
            ViewBag.ShippingCount = await _context.Orders.CountAsync(o => o.Status == "shipping");
            ViewBag.DeliveredCount = await _context.Orders.CountAsync(o => o.Status == "delivered");
            ViewBag.CancelledCount = await _context.Orders.CountAsync(o => o.Status == "cancelled");
            
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            var orders = await PaginatedList<Order>.CreateAsync(ordersQuery, pageNumber ?? 1, pageSize);
            
            return View("~/Views/Manager/OrderManagement.cshtml", orders);
        }

        // GET: /OrderManagement/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View("~/Views/Manager/OrderDetails.cshtml", order);
        }

        // GET: /OrderManagement/ExportExcel
        public async Task<IActionResult> ExportExcel(string? status)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            var orders = await ordersQuery.ToListAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Đơn hàng");

            // Header row styling
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#4f46e5");
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Headers
            worksheet.Cell(1, 1).Value = "Mã đơn";
            worksheet.Cell(1, 2).Value = "Khách hàng";
            worksheet.Cell(1, 3).Value = "Số điện thoại";
            worksheet.Cell(1, 4).Value = "Địa chỉ";
            worksheet.Cell(1, 5).Value = "Ngày đặt";
            worksheet.Cell(1, 6).Value = "Tổng tiền";
            worksheet.Cell(1, 7).Value = "Thanh toán";
            worksheet.Cell(1, 8).Value = "Trạng thái TT";
            worksheet.Cell(1, 9).Value = "Trạng thái đơn";

            // Data rows
            int row = 2;
            foreach (var order in orders)
            {
                var statusText = order.Status switch
                {
                    "pending" => "Chờ xác nhận",
                    "processing" => "Đang xử lý",
                    "shipping" => "Đang giao",
                    "delivered" => "Đã giao",
                    "cancelled" => "Đã hủy",
                    _ => order.Status ?? ""
                };
                var paymentStatusText = order.PaymentStatus switch
                {
                    "paid" => "Đã thanh toán",
                    "unpaid" => "Chưa thanh toán",
                    "refunded" => "Đã hoàn tiền",
                    _ => order.PaymentStatus ?? ""
                };
                var paymentMethodText = order.PaymentMethod switch
                {
                    "cod" => "COD",
                    "vietqr" => "Chuyển khoản",
                    "vnpay" => "VNPay",
                    _ => order.PaymentMethod ?? ""
                };

                worksheet.Cell(row, 1).Value = $"#{order.Id:D6}";
                worksheet.Cell(row, 2).Value = order.FullName ?? "";
                worksheet.Cell(row, 3).Value = order.PhoneNumber ?? "";
                worksheet.Cell(row, 4).Value = order.Address ?? "";
                worksheet.Cell(row, 5).Value = order.CreatedAt?.ToString("dd/MM/yyyy HH:mm") ?? "";
                worksheet.Cell(row, 6).Value = order.TotalMoney;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0";
                worksheet.Cell(row, 7).Value = paymentMethodText;
                worksheet.Cell(row, 8).Value = paymentStatusText;
                worksheet.Cell(row, 9).Value = statusText;

                // Alternate row coloring
                if (row % 2 == 0)
                {
                    worksheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#f8fafc");
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add border to data range
            var dataRange = worksheet.Range(1, 1, row - 1, 9);
            dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Generate filename with timestamp
            var fileName = $"DonHang_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            return File(stream.ToArray(), 
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", 
                fileName);
        }

        // POST: /OrderManagement/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            // Validate status transition
            var validStatuses = new[] { "pending", "processing", "shipping", "delivered", "cancelled" };
            if (!validStatuses.Contains(status))
            {
                return Json(new { success = false, message = "Trạng thái không hợp lệ" });
            }

            // Update payment status if delivered
            if (status == "delivered" && order.PaymentMethod == "cod")
            {
                order.PaymentStatus = "paid";
            }

            order.Status = status;
            
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Cập nhật trạng thái thành công";
            TempData["ToastType"] = "success";

            return Json(new { success = true, newStatus = status, message = "Cập nhật trạng thái thành công" });
        }

        // POST: /OrderManagement/UpdatePaymentStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int id, string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var validStatuses = new[] { "unpaid", "paid", "refunded" };
            if (!validStatuses.Contains(paymentStatus))
            {
                return Json(new { success = false, message = "Trạng thái thanh toán không hợp lệ" });
            }

            order.PaymentStatus = paymentStatus;
            
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Cập nhật thanh toán thành công";
            TempData["ToastType"] = "success";

            return Json(new { success = true, newStatus = paymentStatus, message = "Cập nhật thanh toán thành công" });
        }
    }
}

