using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;
using LaptopStore.Extensions;

namespace LaptopStore.Controllers
{
    public class WishlistController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public WishlistController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Toggle(int productId)
        {
            int? userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập", requireLogin = true });
            }

            var product = _context.Products.FirstOrDefault(p => p.Id == productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var wishlist = _context.Wishlists
                .Include(w => w.WishlistItems)
                .FirstOrDefault(w => w.UserId == userId);

            if (wishlist == null)
            {
                wishlist = new Wishlist
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    WishlistItems = new List<WishlistItem>()
                };
                _context.Wishlists.Add(wishlist);
                _context.SaveChanges();
            }

            var existingItem = wishlist.WishlistItems.FirstOrDefault(wi => wi.ProductId == productId);
            bool isAdded;

            if (existingItem != null)
            {
                _context.WishlistItems.Remove(existingItem);
                isAdded = false;
            }
            else
            {
                wishlist.WishlistItems.Add(new WishlistItem
                {
                    WishlistId = wishlist.Id,
                    ProductId = productId,
                    CreatedAt = DateTime.UtcNow
                });
                isAdded = true;
            }

            _context.SaveChanges();

            var wishlistCount = wishlist.WishlistItems.Count;

            return Json(new
            {
                success = true,
                isInWishlist = isAdded,
                wishlistCount = wishlistCount,
                message = isAdded ? "Đã thêm vào yêu thích" : "Đã xóa khỏi yêu thích"
            });
        }

        [HttpGet]
        public IActionResult GetWishlistCount()
        {
            int? userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Json(new { count = 0 });
            }

            var count = _context.WishlistItems
                .Count(wi => wi.Wishlist != null && wi.Wishlist.UserId == userId);

            return Json(new { count });
        }

        [HttpGet]
        public IActionResult GetWishlistProductIds()
        {
            int? userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return Json(new { productIds = new List<int>() });
            }

            var productIds = _context.WishlistItems
                .Where(wi => wi.Wishlist != null && wi.Wishlist.UserId == userId)
                .Select(wi => wi.ProductId)
                .ToList();

            return Json(new { productIds });
        }

        public IActionResult WishlistView()
        {
            int? userId = User.GetUserId();
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            var wishlist = _context.Wishlists
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefault(w => w.UserId == userId);

            if (wishlist == null)
            {
                wishlist = new Wishlist { WishlistItems = new List<WishlistItem>() };
            }

            return View(wishlist);
        }
    }
}
