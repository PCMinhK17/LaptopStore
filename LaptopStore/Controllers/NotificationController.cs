using LaptopStore.Utils;
using LaptopStore.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaptopStore.Controllers;

public class NotificationController : Controller
{

    private readonly LaptopStoreDbContext _context;

    public NotificationController(LaptopStoreDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var userId = User.GetUserId();
        if (userId == null) return NotFound();

        var notifications = _context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToList();

        var unreadNotificationIds = notifications
            .Where(n => !n.IsRead)
            .Select(n => n.Id)
            .ToList();

        ViewBag.UnreadIds = notifications.Where(n => !n.IsRead).Select(n => n.Id).ToList();

        if (unreadNotificationIds.Any())
        {
            var toUpdate = _context.Notifications
                .Where(n => unreadNotificationIds.Contains(n.Id))
                .ToList();

            foreach (var n in toUpdate)
            {
                n.IsRead = true;
            }

            _context.SaveChanges();
        }

        return View("~/Views/Home/Notification.cshtml", notifications);
    }

    public IActionResult GetUnreadNotificationCount()
    {
        int? userId = User.GetUserId();
        if (!userId.HasValue)
        {
            return Json(new { count = 0 });
        }

        var count = _context.Notifications
            .Count(n => n.UserId == userId && !n.IsRead);

        return Json(new { count });
    }

}
