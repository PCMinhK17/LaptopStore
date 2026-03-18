using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ILogger<ProductController> _logger;
        private readonly LaptopStoreDbContext _context;
        public ReviewController(LaptopStoreDbContext context)
        {
            _context = context;
        }
        [HttpPost]
        public IActionResult CreateReview(int ProductId, int Rating, string Comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["ErrorMessage"] = "Bạn cần đăng nhập để đánh giá.";
                return RedirectToAction("Detail", "Product", new { id = ProductId });
            }

            int uid = userId.Value;

            bool hasPurchased = _context.OrderDetails
                .Any(o => o.ProductId == ProductId
                       && o.Order.UserId == uid
                       && o.Order.Status == "completed");

            if (!hasPurchased)
            {
                TempData["ErrorMessage"] = "Bạn phải mua sản phẩm trước khi đánh giá.";
                return RedirectToAction("Detail", "Product", new { id = ProductId });
            }

            bool hasReviewed = _context.Reviews
                .Any(r => r.ProductId == ProductId && r.UserId == uid);

            if (hasReviewed)
            {
                TempData["ErrorMessage"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Detail", "Product", new { id = ProductId });
            }
            if (Rating < 1 || Rating > 5)
            {
                TempData["ErrorMessage"] = "Rating không hợp lệ.";
                return RedirectToAction("Detail", "Product", new { id = ProductId });
            }
            var review = new Review
            {
                ProductId = ProductId,
                UserId = uid,
                Rating = Rating,
                Comment = Comment,
                CreatedAt = DateTime.Now
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Đánh giá thành công!";

            return RedirectToAction("Detail", "Product", new { id = ProductId });
        }
    }
}
