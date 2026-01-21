using LaptopStore.Models;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.ProductDTOs;

public class AddNewProductRequest
{

    //[Required(ErrorMessage = "Name can't be empty")]
    //[StringLength(99, ErrorMessage = "Name must have under 100 characters")]
    [Required(ErrorMessage = "Vui lòng điền tên cho laptop")]
    [StringLength(100, ErrorMessage = "Tên không được dài quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Mã vạch cho laptop")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phân loại cho laptop")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu cho laptop")]
    public int? BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu cho laptop")]
    public string? Price { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CPU cho laptop")]
    public string? Cpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập RAM cho laptop")]
    public string? Ram { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Ổ cứng cho laptop")]
    public string? HardDrive { get; set; }

    public string? Gpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Kích cỡ màn hình cho laptop")]
    public string? ScreenSize { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Cân nặng cho laptop")]
    public string? Weight { get; set; }

    [Required(ErrorMessage = "Vui lòng tải ảnh thumbnail cho laptop")]
    public IFormFile? ImageFile { get; set; }
}
