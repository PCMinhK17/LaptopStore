using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LaptopStore.Models;

public partial class Product
{
    public int Id { get; set; }

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

    [Range(1000, 100000000, ErrorMessage = "Giá phải từ 1,000 đến 100,000,000")]
    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int? StockQuantity { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập CPU cho laptop")]
    public string? Cpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập RAM cho laptop")]
    public string? Ram { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Ổ cứng cho laptop")]
    public string? HardDrive { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập GPU cho laptop")]
    public string? Gpu { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Kích cỡ màn hình cho laptop")]
    public string? ScreenSize { get; set; }

    public string? Weight { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Brand? Brand { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<ImportDetail> ImportDetails { get; set; } = new List<ImportDetail>();

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
