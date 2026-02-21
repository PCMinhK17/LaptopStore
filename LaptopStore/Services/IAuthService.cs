using LaptopStore.Models;
using LaptopStore.Models.ViewModels;

namespace LaptopStore.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string identifier, string password, bool rememberMe);
        Task<AuthResult> RegisterAsync(RegisterViewModel model);
        Task<bool> CheckEmailExistsAsync(string email);
        Task<bool> CheckPhoneExistsAsync(string phoneNumber);
        Task<User?> GetUserByIdentifierAsync(string identifier);
        Task<bool> VerifyPasswordAsync(User user, string password);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public User? User { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
    }
}
