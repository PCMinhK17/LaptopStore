using System.Security.Claims;
using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using LaptopStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaptopStore.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IAuthService authService,
            IEmailService emailService,
            LaptopStoreDbContext context,
            ILogger<AccountController> logger)
        {
            _authService = authService;
            _emailService = emailService;
            _context = context;
            _logger = logger;
        }

        #region Login

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Redirect nếu đã đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToLocal(returnUrl);
            }

            // Clear TempData khi vào trang mới (tránh hiển thị toast từ lần trước)
            TempData.Clear();

            var model = new LoginViewModel { ReturnUrl = returnUrl };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model.Input, model.Password, model.RememberMe);

            if (!result.Success)
            {
                // Xử lý trường hợp chưa xác thực email
                if (result.RequiresEmailVerification && result.User != null)
                {
                    // Chuyển hướng đến trang thông báo đã gửi email
                    TempData["UserEmail"] = result.User.Email;
                    TempData["UserId"] = result.User.Id;
                    TempData["ToastMessage"] = result.ErrorMessage;
                    TempData["ToastType"] = "warning";
                    return RedirectToAction("VerificationSent");
                }

                // Chỉ hiển thị thông báo đơn giản, không có dấu *
                var errorMessage = result.ErrorMessage?.Contains("Tài khoản") == true 
                    ? "Tài khoản hoặc mật khẩu không chính xác"
                    : result.ErrorMessage ?? "Tài khoản hoặc mật khẩu không chính xác";
                
                ModelState.AddModelError(string.Empty, errorMessage);
                TempData["ToastMessage"] = errorMessage;
                TempData["ToastType"] = "error";
                return View(model);
            }

            if (result.User == null)
            {
                ModelState.AddModelError(string.Empty, "Không thể xác thực người dùng.");
                return View(model);
            }

            // Đăng nhập thành công
            await SignInUserAsync(result.User, model.RememberMe);

            TempData["ToastMessage"] = $"Đăng nhập thành công! Chào mừng {result.User.FullName ?? result.User.Email}";
            TempData["ToastType"] = "success";

            // Redirect theo role hoặc returnUrl
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction(
                result.RedirectAction ?? "Index",
                result.RedirectController ?? "Home");
        }

        #endregion

        #region Register

        [HttpGet]
        public IActionResult Register()
        {
            // Redirect nếu đã đăng nhập
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Product");
            }

            // Clear TempData khi vào trang mới (tránh hiển thị toast từ lần trước)
            TempData.Clear();

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);

            if (!result.Success)
            {
                // Thêm lỗi vào ModelState với field cụ thể nếu có thể
                if (result.ErrorMessage?.Contains("Email") == true)
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.Email), result.ErrorMessage);
                }
                else if (result.ErrorMessage?.Contains("Số điện thoại") == true)
                {
                    ModelState.AddModelError(nameof(RegisterViewModel.PhoneNumber), result.ErrorMessage);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng ký thất bại.");
                }

                TempData["ToastMessage"] = result.ErrorMessage ?? "Đăng ký thất bại.";
                TempData["ToastType"] = "error";
                return View(model);
            }

            if (result.User == null)
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản.");
                return View(model);
            }

            // Tạo token xác thực email và gửi mail
            if (result.RequiresEmailVerification)
            {
                try
                {
                    var token = await _authService.GenerateEmailVerificationTokenAsync(result.User.Id);
                    var verificationLink = Url.Action(
                        "VerifyEmail",
                        "Account",
                        new { token },
                        Request.Scheme
                    );

                    await _emailService.SendVerificationEmailAsync(
                        result.User.Email,
                        result.User.FullName ?? result.User.Email,
                        verificationLink ?? ""
                    );

                    TempData["UserEmail"] = result.User.Email;
                    TempData["UserId"] = result.User.Id;

                    return RedirectToAction("VerificationSent");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send verification email");
                    TempData["ToastMessage"] = "Đăng ký thành công nhưng không thể gửi email xác thực. Vui lòng thử gửi lại.";
                    TempData["ToastType"] = "warning";
                    TempData["UserEmail"] = result.User.Email;
                    TempData["UserId"] = result.User.Id;
                    return RedirectToAction("VerificationSent");
                }
            }

            // Nếu không cần xác thực (để cho tương lai, có thể disable tính năng này)
            await SignInUserAsync(result.User, false);

            TempData["ToastMessage"] = $"Đăng ký thành công! Chào mừng {result.User.FullName ?? result.User.Email}";
            TempData["ToastType"] = "success";

            return RedirectToAction(
                result.RedirectAction ?? "Index",
                result.RedirectController ?? "Product");
        }

        #endregion

        #region Email Verification

        [HttpGet]
        public IActionResult VerificationSent()
        {
            var email = TempData["UserEmail"]?.ToString();
            var userId = TempData["UserId"];

            // Giữ lại TempData để có thể dùng khi gửi lại
            TempData.Keep("UserEmail");
            TempData.Keep("UserId");

            ViewBag.Email = email;
            ViewBag.UserId = userId;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> VerifyEmail(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                TempData["ToastMessage"] = "Link xác thực không hợp lệ.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }

            var result = await _authService.VerifyEmailTokenAsync(token);

            if (!result.Success)
            {
                TempData["ToastMessage"] = result.ErrorMessage ?? "Xác thực email thất bại.";
                TempData["ToastType"] = "error";
                return RedirectToAction("VerificationFailed", new { message = result.ErrorMessage });
            }

            // Gửi email thông báo xác thực thành công
            if (result.User != null)
            {
                await _emailService.SendVerificationSuccessEmailAsync(
                    result.User.Email,
                    result.User.FullName ?? result.User.Email
                );
            }

            TempData["ToastMessage"] = "Xác thực email thành công! Bạn có thể đăng nhập ngay bây giờ.";
            TempData["ToastType"] = "success";

            return RedirectToAction("VerificationSuccess");
        }

        [HttpGet]
        public IActionResult VerificationSuccess()
        {
            return View();
        }

        [HttpGet]
        public IActionResult VerificationFailed(string? message)
        {
            ViewBag.ErrorMessage = message ?? "Xác thực email thất bại.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendVerificationEmail(int userId)
        {
            var user = await _authService.GetUserByIdAsync(userId);
            if (user == null)
            {
                TempData["ToastMessage"] = "Không tìm thấy tài khoản.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }

            if (user.Status == "active")
            {
                TempData["ToastMessage"] = "Tài khoản đã được xác thực. Vui lòng đăng nhập.";
                TempData["ToastType"] = "info";
                return RedirectToAction("Login");
            }

            try
            {
                var token = await _authService.GenerateEmailVerificationTokenAsync(user.Id);
                var verificationLink = Url.Action(
                    "VerifyEmail",
                    "Account",
                    new { token },
                    Request.Scheme
                );

                await _emailService.SendVerificationEmailAsync(
                    user.Email,
                    user.FullName ?? user.Email,
                    verificationLink ?? ""
                );

                TempData["ToastMessage"] = "Email xác thực đã được gửi lại. Vui lòng kiểm tra hộp thư của bạn.";
                TempData["ToastType"] = "success";
                TempData["UserEmail"] = user.Email;
                TempData["UserId"] = user.Id;

                return RedirectToAction("VerificationSent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend verification email");
                TempData["ToastMessage"] = "Không thể gửi email xác thực. Vui lòng thử lại sau.";
                TempData["ToastType"] = "error";
                return RedirectToAction("VerificationSent");
            }
        }

        #endregion

        #region Account Setup (for admin created users)

        [HttpGet]
        public async Task<IActionResult> SetupAccount(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                TempData["ToastMessage"] = "Liên kết không hợp lệ.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }
            
             // Validate token without consuming it
            var isValid = await _authService.ValidateEmailTokenAsync(token);
            if (!isValid)
            {
                 TempData["ToastMessage"] = "Mã xác thực không hợp lệ hoặc đã hết hạn.";
                 TempData["ToastType"] = "error";
                 return RedirectToAction("Login");
            }

            var model = new SetupAccountViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupAccount(SetupAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Verify and consume token
            var result = await _authService.VerifyEmailTokenAsync(model.Token);

            if (!result.Success)
            {
                 TempData["ToastMessage"] = result.ErrorMessage ?? "Xác thực thất bại.";
                 TempData["ToastType"] = "error";
                 return RedirectToAction("Login");
            }
            
            var user = result.User;
            if (user == null || user.Email != model.Email)
            {
                 TempData["ToastMessage"] = "Người dùng không hợp lệ.";
                 TempData["ToastType"] = "error";
                 return RedirectToAction("Login");
            }

            // Update password
            user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            // Status is set to "active" by VerifyEmailTokenAsync already
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["ToastMessage"] = "Thiết lập tài khoản thành công. Vui lòng đăng nhập.";
            TempData["ToastType"] = "success";

            return RedirectToAction("Login");
        }

        #endregion

        #region Validation APIs (for real-time validation)

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { exists = false, valid = false });
            }

            var normalized = email.Trim().ToLowerInvariant();

            // Validate email format
            var emailRegex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
            var isValid = emailRegex.IsMatch(normalized);

            if (!isValid)
            {
                return Json(new { exists = false, valid = false });
            }

            var exists = await _authService.CheckEmailExistsAsync(normalized);
            return Json(new { exists, valid = true });
        }

        [HttpGet]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckPhone(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Json(new { exists = false, valid = false });
            }

            var normalized = phoneNumber.Trim();

            // Validate phone format (9-11 digits)
            var phoneRegex = new System.Text.RegularExpressions.Regex(@"^[0-9]{9,11}$");
            var isValid = phoneRegex.IsMatch(normalized);

            if (!isValid)
            {
                return Json(new { exists = false, valid = false });
            }

            var exists = await _authService.CheckPhoneExistsAsync(normalized);
            return Json(new { exists, valid = true });
        }

        #endregion

        #region Google OAuth

        [HttpGet]
        public IActionResult GoogleLogin()
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = "/Account/GoogleResponse"
            };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleResponse()
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);
                
                if (result?.Principal == null)
                {
                    TempData["ToastMessage"] = "Không thể xác thực với Google. Vui lòng thử lại.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Login");
                }

                var email = result.Principal.FindFirstValue(ClaimTypes.Email);
                var name = result.Principal.FindFirstValue(ClaimTypes.Name);

                if (string.IsNullOrWhiteSpace(email))
                {
                    TempData["ToastMessage"] = "Không thể lấy thông tin email từ Google.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Login");
                }

                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Tạo user mới từ Google
                    var now = DateTime.Now;
                    user = new User
                    {
                        Email = email,
                        FullName = name,
                        Password = string.Empty, // Google OAuth không cần password
                        Role = "customer",
                        Status = "active",
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("New user registered via Google: {Email}", email);
                }

                // Đăng nhập
                await SignInUserAsync(user, false);

                TempData["ToastMessage"] = $"Đăng nhập bằng Google thành công! Chào mừng {user.FullName ?? user.Email}";
                TempData["ToastType"] = "success";

                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Google OAuth");
                TempData["ToastMessage"] = "Có lỗi xảy ra khi đăng nhập bằng Google. Vui lòng thử lại.";
                TempData["ToastType"] = "error";
                return RedirectToAction("Login");
            }
        }

        #endregion

        #region Logout

        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            try
            {
                // Xóa authentication cookie
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                // Xóa tất cả session data
                HttpContext.Session.Clear();

                // Xóa tất cả cookies liên quan
                foreach (var cookie in Request.Cookies.Keys)
                {
                    Response.Cookies.Delete(cookie);
                }

                _logger.LogInformation("User logged out successfully");

                TempData["ToastMessage"] = "Bạn đã đăng xuất thành công.";
                TempData["ToastType"] = "success";

                return RedirectToAction("Login", "Account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                
                // Vẫn redirect về login dù có lỗi
                return RedirectToAction("Login", "Account");
            }
        }

        #endregion

        #region Helper Methods

        private async Task SignInUserAsync(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "customer"),
                new Claim("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(30) : null
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Product");
        }

        #endregion

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }
    }
}
