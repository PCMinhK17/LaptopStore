using System;
using System.IO;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LaptopStore.Extensions;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    public class UserController : Controller
    {
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<UserController> _logger;
        private readonly IWebHostEnvironment _env;

        public UserController(
            LaptopStoreDbContext context, 
            ILogger<UserController> logger,
            IWebHostEnvironment env)
        {
            _context = context;
            _logger = logger;
            _env = env;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Profile(bool edit = false)
        {
            int? userId = User.GetUserId();

            if (userId == null)
            {
                TempData["ToastMessage"] = "Vui lòng đăng nhập để xem thông tin";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy người dùng";
                TempData["ToastType"] = "error";
                return RedirectToAction("Index", "Home");
            }

            ViewBag.EditMode = edit;
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(User model, IFormFile? avatar)
        {
            int? userId = User.GetUserId();

            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập lại" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            try
            {
                // Validate FullName
                if (string.IsNullOrWhiteSpace(model.FullName))
                {
                    return Json(new { success = false, message = "Họ và tên không được để trống" });
                }

                if (model.FullName.Length < 2 || model.FullName.Length > 100)
                {
                    return Json(new { success = false, message = "Họ và tên phải từ 2 đến 100 ký tự" });
                }

                // Validate PhoneNumber (optional but if provided must be valid)
                if (!string.IsNullOrWhiteSpace(model.PhoneNumber))
                {
                    var phoneRegex = new Regex(@"^[0-9]{9,11}$");
                    if (!phoneRegex.IsMatch(model.PhoneNumber))
                    {
                        return Json(new { success = false, message = "Số điện thoại không hợp lệ (9-11 số)" });
                    }
                }

                // Handle avatar upload
                if (avatar != null && avatar.Length > 0)
                {
                    // Validate file type
                    var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                    if (!allowedTypes.Contains(avatar.ContentType.ToLowerInvariant()))
                    {
                        return Json(new { success = false, message = "Chỉ chấp nhận file ảnh (JPEG, PNG, GIF, WebP)" });
                    }

                    // Validate file size (max 2MB)
                    if (avatar.Length > 2 * 1024 * 1024)
                    {
                        return Json(new { success = false, message = "Kích thước ảnh không được vượt quá 2MB" });
                    }

                    // Create upload directory
                    var uploadsPath = Path.Combine(_env.ContentRootPath, "wwwroot", "uploads", "avatars");
                    if (!Directory.Exists(uploadsPath))
                    {
                        Directory.CreateDirectory(uploadsPath);
                    }

                    // Generate unique filename
                    var extension = Path.GetExtension(avatar.FileName).ToLowerInvariant();
                    var fileName = $"avatar_{user.Id}_{DateTime.Now.Ticks}{extension}";
                    var filePath = Path.Combine(uploadsPath, fileName);

                    // Delete old avatar if exists
                    if (!string.IsNullOrEmpty(user.AvatarUrl))
                    {
                        var oldFilePath = Path.Combine(uploadsPath, user.AvatarUrl);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            System.IO.File.Delete(oldFilePath);
                        }
                    }

                    // Save new avatar
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await avatar.CopyToAsync(stream);
                    }

                    user.AvatarUrl = fileName;
                }

                // Update user info
                user.FullName = model.FullName?.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();
                user.Address = model.Address?.Trim();
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} updated profile successfully", userId);

                return Json(new { 
                    success = true, 
                    message = "Cập nhật thông tin thành công!",
                    data = new {
                        fullName = user.FullName,
                        avatarUrl = user.AvatarUrl
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        #region Change Password

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                int? userId = User.GetUserId();

                if (userId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập lại" });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(currentPassword))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mật khẩu hiện tại" });
                }

                if (string.IsNullOrWhiteSpace(newPassword))
                {
                    return Json(new { success = false, message = "Vui lòng nhập mật khẩu mới" });
                }

                if (string.IsNullOrWhiteSpace(confirmPassword))
                {
                    return Json(new { success = false, message = "Vui lòng xác nhận mật khẩu mới" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "Mật khẩu xác nhận không khớp" });
                }

                // Password validation regex: at least 8 chars, contains uppercase, lowercase, and number
                var passwordRegex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$");
                if (!passwordRegex.IsMatch(newPassword))
                {
                    return Json(new { success = false, message = "Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số" });
                }

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Skip password check if user logged in via Google (empty password)
                if (!string.IsNullOrEmpty(user.Password))
                {
                    if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.Password))
                    {
                        return Json(new { success = false, message = "Mật khẩu hiện tại không chính xác" });
                    }
                }

                // Check if new password is same as current
                if (BCrypt.Net.BCrypt.Verify(newPassword, user.Password))
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải khác mật khẩu hiện tại" });
                }

                // Update password
                user.Password = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} changed password successfully", userId);

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        #endregion

        #region Get User Info API

        [HttpGet]
        public async Task<IActionResult> GetUserInfo()
        {
            int? userId = User.GetUserId();
            if (userId == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            return Json(new
            {
                success = true,
                data = new
                {
                    fullName = user.FullName,
                    email = user.Email,
                    phoneNumber = user.PhoneNumber,
                    address = user.Address,
                    avatarUrl = user.AvatarUrl,
                    createdAt = user.CreatedAt?.ToString("dd/MM/yyyy")
                }
            });
        }

        #endregion
    }
}
