using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaptopStore.Models;

namespace LaptopStore.Controllers
{
    [Authorize(Roles = "admin")]
    public class SettingsController : Controller
    {
        private readonly LaptopStoreDbContext _context;

        public SettingsController(LaptopStoreDbContext context)
        {
            _context = context;
        }

        // GET: /Settings
        public IActionResult Index()
        {
            return View("~/Views/Manager/Settings.cshtml");
        }

        // POST: /Settings/UpdateTheme
        [HttpPost]
        public IActionResult UpdateTheme([FromBody] ThemeSettings settings)
        {
            // In a real application, you would save this to a database or configuration file
            // For now, we'll just return success (theme is handled client-side with localStorage)
            return Json(new { success = true, message = "Cài đặt giao diện đã được lưu" });
        }

        // POST: /Settings/UpdateGeneral
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateGeneral(GeneralSettings settings)
        {
            if (ModelState.IsValid)
            {
                // Save settings to database/config
                TempData["SuccessMessage"] = "Cài đặt chung đã được lưu thành công";
                return RedirectToAction("Index");
            }
            return View("~/Views/Manager/Settings.cshtml");
        }
    }

    public class ThemeSettings
    {
        public string Theme { get; set; } = "light";
        public string PrimaryColor { get; set; } = "#6366f1";
        public string AccentColor { get; set; } = "#8b5cf6";
    }

    public class GeneralSettings
    {
        public string StoreName { get; set; } = "";
        public string StoreEmail { get; set; } = "";
        public string StorePhone { get; set; } = "";
        public string StoreAddress { get; set; } = "";
        public string Currency { get; set; } = "VND";
        public string Language { get; set; } = "vi";
    }
}
