using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LaptopStore.Services
{
    public class AuthService : IAuthService
    {
        private readonly LaptopStoreDbContext _context;
        private readonly ILogger<AuthService> _logger;

        public AuthService(LaptopStoreDbContext context, ILogger<AuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<AuthResult> LoginAsync(string identifier, string password, bool rememberMe)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(password))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Vui lòng nhập đầy đủ thông tin."
                    };
                }

                var user = await GetUserByIdentifierAsync(identifier.Trim());

                // Security: Không phân biệt user không tồn tại hay password sai
                bool isPasswordValid = false;
                if (user != null)
                {
                    isPasswordValid = await VerifyPasswordAsync(user, password);
                }

                if (user == null || !isPasswordValid)
                {
                    _logger.LogWarning("Login failed for identifier: {Identifier}", identifier);
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác"
                    };
                }

                // Kiểm tra trạng thái tài khoản
                if (!string.Equals(user.Status, "active", StringComparison.OrdinalIgnoreCase))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Tài khoản hoặc mật khẩu không chính xác"
                    };
                }

                // Xác định redirect dựa trên role
                var (controller, action) = GetRedirectByRole(user.Role);

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    RedirectController = controller,
                    RedirectAction = action
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for identifier: {Identifier}", identifier);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi hệ thống. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<AuthResult> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var email = model.Email?.Trim() ?? string.Empty;
                var phone = model.PhoneNumber?.Trim() ?? string.Empty;

                // Kiểm tra email đã tồn tại
                if (await CheckEmailExistsAsync(email))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Email này đã được sử dụng. Vui lòng sử dụng email khác."
                    };
                }

                // Kiểm tra số điện thoại đã tồn tại
                if (await CheckPhoneExistsAsync(phone))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Số điện thoại này đã được sử dụng. Vui lòng sử dụng số điện thoại khác."
                    };
                }

                var now = DateTime.Now;

                var user = new User
                {
                    FullName = model.FullName?.Trim(),
                    Email = email,
                    PhoneNumber = phone,
                    Address = model.Address?.Trim(),
                    Password = BCryptNet.HashPassword(model.Password),
                    Role = "customer",
                    Status = "active",
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New user registered: {Email}", email);

                return new AuthResult
                {
                    Success = true,
                    User = user,
                    RedirectController = "Product",
                    RedirectAction = "Index"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau."
                };
            }
        }

        public async Task<bool> CheckEmailExistsAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return await _context.Users.AnyAsync(u => u.Email == email.Trim());
        }

        public async Task<bool> CheckPhoneExistsAsync(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            return await _context.Users.AnyAsync(u => u.PhoneNumber == phoneNumber.Trim());
        }

        public async Task<User?> GetUserByIdentifierAsync(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                return null;

            var normalized = identifier.Trim();

            if (normalized.Contains("@"))
            {
                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == normalized);
            }
            else
            {
                return await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.PhoneNumber == normalized);
            }
        }

        public Task<bool> VerifyPasswordAsync(User user, string password)
        {
            if (user == null || string.IsNullOrWhiteSpace(password))
                return Task.FromResult(false);

            try
            {
                // Thử verify với BCrypt
                var isValid = BCryptNet.Verify(password, user.Password);
                return Task.FromResult(isValid);
            }
            catch
            {
                // Fallback cho password cũ (plain text - không nên dùng trong production)
                if (user.Password == password)
                    return Task.FromResult(true);

                return Task.FromResult(false);
            }
        }

        private (string controller, string action) GetRedirectByRole(string? role)
        {
            return role?.ToLower() switch
            {
                "admin" => ("ProductManagement", "Index"),
                "staff" => ("Home", "Index"),
                "customer" => ("Product", "Index"),
                _ => ("Home", "Index")
            };
        }
    }
}
