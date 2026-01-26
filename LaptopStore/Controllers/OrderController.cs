using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;

namespace LaptopStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public OrderController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        // GET: /Order/Checkout?userId=...
        public IActionResult Checkout(int? userId = 2)
        {
            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .FirstOrDefault(c => c.UserId == userId);
            }
            else
            {
                var cartId = _context.CartItems
                    .Select(ci => (int?)ci.CartId)
                    .FirstOrDefault();

                if (cartId.HasValue)
                {
                    cart = _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                                .ThenInclude(p => p.ProductImages)
                        .FirstOrDefault(c => c.Id == cartId.Value);
                }
            }

            if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
            {
                return RedirectToAction("CartView", "Cart");
            }

            // Trả view Checkout nằm trong thư mục Cart
            return View("~/Views/Cart/Checkout.cshtml", cart);
        }

        // POST: /Order/PlaceOrder
        [HttpPost]
        public IActionResult PlaceOrder(int? userId, string fullName, string phoneNumber, string email,
            string province, string district, string address, string note, string paymentMethod)
        {
            // Normalize paymentMethod to DB allowed values
            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                var pm = paymentMethod.Trim().ToLowerInvariant();
                if (pm == "cod" || pm == "cash" || pm.Contains("cash") || pm.Contains("cod"))
                    paymentMethod = "cod";
                else if (pm.Contains("bank") || pm.Contains("vietqr") || pm.Contains("qr") || pm.Contains("transfer"))
                    paymentMethod = "vietqr";
                else if (pm.Contains("vnpay"))
                    paymentMethod = "vnpay";
                else
                    paymentMethod = pm;
            }
            else
            {
                paymentMethod = "cod";
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Lấy cart và load sản phẩm
                Cart? cart = null;
                if (userId.HasValue)
                {
                    cart = _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                        .FirstOrDefault(c => c.UserId == userId);
                }
                else
                {
                    var cartId = _context.CartItems
                        .Select(ci => (int?)ci.CartId)
                        .FirstOrDefault();

                    if (cartId.HasValue)
                    {
                        cart = _context.Carts
                            .Include(c => c.CartItems)
                                .ThenInclude(ci => ci.Product)
                            .FirstOrDefault(c => c.Id == cartId.Value);
                    }
                }

                if (cart == null || cart.CartItems == null || !cart.CartItems.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống" });
                }

                // Kiểm tra tồn kho trước khi tạo đơn
                foreach (var ci in cart.CartItems)
                {
                    var prod = ci.Product;
                    var qty = ci.Quantity ?? 1;
                    if (prod == null)
                    {
                        return Json(new { success = false, message = $"Sản phẩm (id {ci.ProductId}) không tồn tại" });
                    }
                    if (prod.StockQuantity.HasValue && qty > prod.StockQuantity.Value)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Sản phẩm \"{prod.Name}\" chỉ còn {prod.StockQuantity.Value} cái",
                            productId = prod.Id,
                            maxQuantity = prod.StockQuantity.Value
                        });
                    }
                }

                // Tính toán giá
                var subtotal = cart.CartItems.Sum(ci => (ci.Quantity ?? 0) * (ci.Product?.Price ?? 0));

                // Shipping mặc định miễn phí
                decimal shippingFee = 0m;

                var totalMoney = subtotal + shippingFee;
                var fullAddress = $"{address}, {district}, {province}";

                // Tạo order (gán status và paymentStatus theo mã DB)
                var order = new Order
                {
                    UserId = userId,
                    Subtotal = subtotal,
                    ShippingFee = shippingFee,
                    TotalMoney = totalMoney,
                    FullName = fullName,
                    PhoneNumber = phoneNumber,
                    Address = fullAddress,
                    Note = note,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = "unpaid", // phù hợp với DB ('unpaid','paid','refunded')
                    Status = "pending",       // phù hợp với CHECK constraint
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                _context.SaveChanges(); // để lấy order.Id

                // Tạo order details và cập nhật tồn kho
                foreach (var cartItem in cart.CartItems)
                {
                    var prod = cartItem.Product!;
                    var qty = cartItem.Quantity ?? 1;

                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = qty,
                        Price = prod.Price
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Cập nhật tồn kho nếu có
                    if (prod.StockQuantity.HasValue)
                    {
                        prod.StockQuantity -= qty;
                    }
                }

                // Xóa cart items
                _context.CartItems.RemoveRange(cart.CartItems);

                _context.SaveChanges();
                transaction.Commit();

                var processingDays = 1;
                var shippingMinDays = 3;
                var shippingMaxDays = 5;
                var today = DateTime.Now.Date;
                var estFrom = today.AddDays(processingDays + shippingMinDays);
                var estTo = today.AddDays(processingDays + shippingMaxDays);

                var redirectUrl = Url.Action("Details", "Order", new { id = order.Id });

                // Nếu request AJAX -> trả JSON, ngược lại redirect (trình duyệt sẽ follow)
                var isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                             || Request.Headers["Accept"].ToString().Contains("application/json");

                if (isAjax)
                {
                    return Json(new
                    {
                        success = true,
                        orderId = order.Id,
                        redirectUrl = redirectUrl,
                        estimatedFrom = estFrom.ToString("yyyy-MM-dd"),
                        estimatedTo = estTo.ToString("yyyy-MM-dd"),
                        message = "Đặt hàng thành công!"
                    });
                }

                return Redirect(redirectUrl);
            }
            catch (DbUpdateException dbEx)
            {
                transaction.Rollback();
                var inner = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, message = inner });
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: /Order/Details/5
        public IActionResult Details(int id)
        {
            var order = _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                        .ThenInclude(p => p.ProductImages)
                .FirstOrDefault(o => o.Id == id);

            if (order == null)
                return NotFound();

            // Estimated delivery same calculation
            var processingDays = 1;
            var shippingMinDays = 3;
            var shippingMaxDays = 5;
            var today = DateTime.Now.Date;
            ViewBag.EstFrom = today.AddDays(processingDays + shippingMinDays);
            ViewBag.EstTo = today.AddDays(processingDays + shippingMaxDays);

            return View(order);
        }
    }
}