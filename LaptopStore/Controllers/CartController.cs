using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;

namespace LaptopStore.Controllers
{
    public class CartController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public CartController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        public IActionResult CartView(int? userId = 2)
        {
            // Nếu không có userId, có thể lấy từ session hoặc authentication
            // Tạm thời lấy cart đầu tiên hoặc cart của user đầu tiên
            Cart? cart = null;

            if (userId.HasValue)
            {
                cart = _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Brand)
                    .FirstOrDefault(c => c.UserId == userId);
            }
            else
            {
                // Lấy cart đầu tiên có dữ liệu (để demo)
                // Tìm cart có cart items
                var cartId = _context.CartItems
                    .Select(ci => ci.CartId)
                    .FirstOrDefault();
                
                if (cartId.HasValue)
                {
                    cart = _context.Carts
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                                .ThenInclude(p => p.ProductImages)
                        .Include(c => c.CartItems)
                            .ThenInclude(ci => ci.Product)
                                .ThenInclude(p => p.Brand)
                        .FirstOrDefault(c => c.Id == cartId.Value);
                }
            }

            // Nếu không có cart, tạo cart rỗng
            if (cart == null)
            {
                cart = new Cart
                {
                    CartItems = new List<CartItem>()
                };
            }

            return View(cart);
        }

        [HttpPost]
        public IActionResult UpdateCartItemQuantity(int cartItemId, int quantity)
        {
            try
            {
                var cartItem = _context.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                if (quantity < 1)
                {
                    quantity = 1;
                }

                // Kiểm tra số lượng tồn kho
                var product = _context.Products.FirstOrDefault(p => p.Id == cartItem.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                if (product.StockQuantity.HasValue && quantity > product.StockQuantity.Value)
                {
                    return Json(new { 
                        success = false, 
                        message = $"Số lượng vượt quá tồn kho. Chỉ còn {product.StockQuantity.Value} sản phẩm",
                        maxQuantity = product.StockQuantity.Value
                    });
                }

                cartItem.Quantity = quantity;
                _context.SaveChanges();

                // Tính lại tổng tiền (product đã được load ở trên)
                var itemTotal = (quantity * (product?.Price ?? 0));

                // Tính tổng đơn hàng
                var cart = _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefault(c => c.Id == cartItem.CartId);

                var totalAmount = cart?.CartItems?.Sum(ci => (ci.Quantity ?? 0) * (ci.Product?.Price ?? 0)) ?? 0;

                return Json(new 
                { 
                    success = true, 
                    itemTotal = itemTotal,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult RemoveCartItem(int cartItemId)
        {
            try
            {
                var cartItem = _context.CartItems.FirstOrDefault(ci => ci.Id == cartItemId);
                if (cartItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                var cartId = cartItem.CartId;
                _context.CartItems.Remove(cartItem);
                _context.SaveChanges();

                // Tính lại tổng đơn hàng
                var cart = _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefault(c => c.Id == cartId);

                var totalAmount = cart?.CartItems?.Sum(ci => (ci.Quantity ?? 0) * (ci.Product?.Price ?? 0)) ?? 0;

                return Json(new 
                { 
                    success = true,
                    totalAmount = totalAmount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

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
                    .Select(ci => ci.CartId)
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
                return RedirectToAction("CartView");
            }

            return View(cart);
        }

        [HttpPost]
        public IActionResult PlaceOrder(int? userId, string fullName, string phoneNumber, string email, 
            string province, string district, string address, string note, string paymentMethod)
        {
            try
            {
                // Lấy cart
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
                        .Select(ci => ci.CartId)
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

                // Tính toán giá
                var subtotal = cart.CartItems.Sum(ci => (ci.Quantity ?? 0) * (ci.Product?.Price ?? 0));
                
                // Tính phí vận chuyển (ví dụ: Hà Nội và TP.HCM miễn phí, các tỉnh khác 30k)
                decimal shippingFee = 0;
                if (!string.IsNullOrEmpty(province))
                {
                    var provinceLower = province.ToLower();
                    if (provinceLower.Contains("hà nội") || provinceLower.Contains("hanoi") ||
                        provinceLower.Contains("tp. hồ chí minh") || provinceLower.Contains("ho chi minh") ||
                        provinceLower.Contains("hồ chí minh") || provinceLower.Contains("tphcm"))
                    {
                        shippingFee = 0;
                    }
                    else
                    {
                        shippingFee = 30000;
                    }
                }

                var totalMoney = subtotal + shippingFee;
                var fullAddress = $"{address}, {district}, {province}";

                // Tạo đơn hàng
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
                    PaymentStatus = paymentMethod == "COD" ? "Chưa thanh toán" : "Chờ xác nhận",
                    Status = "Đang xử lý",
                    CreatedAt = DateTime.Now
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                // Tạo order details
                foreach (var cartItem in cart.CartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        OrderId = order.Id,
                        ProductId = cartItem.ProductId,
                        Quantity = cartItem.Quantity ?? 1,
                        Price = cartItem.Product?.Price ?? 0,
                        TotalPrice = (cartItem.Quantity ?? 1) * (cartItem.Product?.Price ?? 0)
                    };
                    _context.OrderDetails.Add(orderDetail);

                    // Cập nhật số lượng tồn kho
                    if (cartItem.Product != null && cartItem.Product.StockQuantity.HasValue)
                    {
                        cartItem.Product.StockQuantity -= cartItem.Quantity ?? 1;
                    }
                }

                // Xóa giỏ hàng sau khi đặt hàng thành công
                _context.CartItems.RemoveRange(cart.CartItems);
                _context.SaveChanges();

                return Json(new { success = true, orderId = order.Id, message = "Đặt hàng thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CalculateShipping(string province)
        {
            decimal shippingFee = 0;
            if (!string.IsNullOrEmpty(province))
            {
                var provinceLower = province.ToLower();
                if (provinceLower.Contains("hà nội") || provinceLower.Contains("hanoi") ||
                    provinceLower.Contains("tp. hồ chí minh") || provinceLower.Contains("ho chi minh") ||
                    provinceLower.Contains("hồ chí minh") || provinceLower.Contains("tphcm"))
                {
                    shippingFee = 0;
                }
                else
                {
                    shippingFee = 30000;
                }
            }
            return Json(new { shippingFee = shippingFee });
        }
    }
}
