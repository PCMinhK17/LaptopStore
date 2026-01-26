using LaptopStore.Models;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.DTOs.ProductDTOs;

public class AddNewProductRequest
{

    //[Required(ErrorMessage = "Name can't be empty")]
    //[StringLength(99, ErrorMessage = "Name must have under 100 characters")]
    [Required(ErrorMessage = "Vui lòng điền tên cho laptop")]
    [StringLength(255, ErrorMessage = "Tên không được dài quá 255 ký tự")]
    public string Name { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập Mã vạch cho laptop")]
    [StringLength(50, ErrorMessage = "Mã SKU không được dài quá 255 ký tự")]
    public string? Sku { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn phân loại cho laptop")]
    public int? CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu cho laptop")]
    public int? BrandId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thương hiệu cho laptop")]
    public string? Price { get; set; }

    public string? Description { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả ngắn không được dài quá 500 ký tự")]
    public string? ShortDescription { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CPU cho laptop")]
    [StringLength(100, ErrorMessage = "CPU không được dài quá 100 ký tự")]
    public string? Cpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập RAM cho laptop")]
    [StringLength(50, ErrorMessage = "RAM không được dài quá 50 ký tự")]
    public string? Ram { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Ổ cứng cho laptop")]
    [StringLength(100, ErrorMessage = "Ổ cứng không được dài quá 100 ký tự")]
    public string? HardDrive { get; set; }

    [StringLength(100, ErrorMessage = "Card đồ họa không được dài quá 100 ký tự")]
    public string? Gpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Kích cỡ màn hình cho laptop")]
    [StringLength(50, ErrorMessage = "Kích thước màn hình không đưoợc dài quá 50 ký tự")]
    public string? ScreenSize { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Cân nặng cho laptop")]
    [StringLength(50, ErrorMessage = "Cân nặng không được dài quá 50 ký tự")]
    public string? Weight { get; set; }

    [Required(ErrorMessage = "Vui lòng tải ảnh thumbnail cho laptop")]
    public IFormFile? ImageFile { get; set; }
}
