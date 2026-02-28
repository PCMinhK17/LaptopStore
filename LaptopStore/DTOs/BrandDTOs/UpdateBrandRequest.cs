using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.BrandDTOs;

public class UpdateBrandRequest
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu")]
    [StringLength(255, ErrorMessage = "Tên thương hiệu không được dài quá 255 ký tự")]
    public string Name { get; set; } = null!;
    [Required(ErrorMessage = "Vui lòng nhập tên quốc gia")]
    [StringLength(100, ErrorMessage = "Quốc gia không được dài quá 100 ký tự")]
    public string? Origin { get; set; } = null!;
    public IFormFile? LogoFile { get; set; }
}
