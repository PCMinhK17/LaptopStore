using LaptopStore.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Models
{
    public class ReviewController : Controller
    {
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult Submit(int productId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                TempData["ReviewError"] = "Bạn cần đăng nhập.";
                return RedirectToAction("Detail", "Store", new { id = productId });
            }

            // kiểm tra đã mua chưa
            bool hasPurchased = _context.OrderDetails
                .Any(od => od.ProductId == productId &&
                           od.Order.UserId == userId &&
                           od.Order.Status == "Completed");

            if (!hasPurchased)
            {
                TempData["ReviewError"] = "Bạn chỉ có thể đánh giá sản phẩm đã mua.";
                return RedirectToAction("Detail", "Store", new { id = productId });
            }

            // kiểm tra đã review chưa
            bool hasReviewed = _context.Reviews
                .Any(r => r.ProductId == productId && r.UserId == userId);

            if (hasReviewed)
            {
                TempData["ReviewError"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToAction("Detail", "Store", new { id = productId });
            }

            if (!string.IsNullOrEmpty(comment) && comment.Length > 100)
            {
                TempData["ErrorMessage"] = "Comment không được quá 100 ký tự.";
                return RedirectToAction("Detail", "Product", new { id = productId });
            }
            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment,
                CreatedAt = DateTime.Now,
                IsApproved = true
            };

            _context.Reviews.Add(review);
            _context.SaveChanges();

            TempData["ReviewSuccess"] = "Đánh giá của bạn đã được gửi.";

            return RedirectToAction("Detail", "Store", new { id = productId });
        }
    }
}
