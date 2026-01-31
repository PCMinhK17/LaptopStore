using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Email hoặc Số điện thoại")]
        [Display(Name = "Email hoặc Số điện thoại")]
        [StringLength(100, ErrorMessage = "Email hoặc số điện thoại không được vượt quá 100 ký tự")]
        public string Input { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Mật khẩu không hợp lệ")]
        public string Password { get; set; } = null!;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
