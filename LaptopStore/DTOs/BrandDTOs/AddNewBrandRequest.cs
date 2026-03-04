using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.BrandDTOs;

public class AddNewBrandRequest
{
    [Required(ErrorMessage = "Vui lòng nhập tên thương hiệu")]
    [StringLength(255, ErrorMessage = "Tên thương hiệu không được dài quá 255 ký tự")]
    public string Name { get; set; } = null!;

    [StringLength(100, ErrorMessage = "Quốc gia không được dài quá 100 ký tự")]
    public string? Origin { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả không được dài quá 500 ký tự")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Vui lòng tải logo cho thương hiệu")]
    public IFormFile? LogoFile { get; set; }
}
