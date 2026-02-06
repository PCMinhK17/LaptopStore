using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        public string FullName { get; set; }

        [Required]
        public string Role { get; set; } = "customer";
    }

    public class EditUserViewModel
    {
        public int Id { get; set; }
        
        [Required]
        public string Email { get; set; }
        
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        
        [Required]
        public string Role { get; set; }

        public string? NewPassword { get; set; }
    }
}
