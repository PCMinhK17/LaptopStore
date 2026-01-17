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
    }
}
