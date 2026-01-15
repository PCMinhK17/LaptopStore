using System;
using System.Collections.Generic;

namespace LaptopStore.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Sku { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public decimal Price { get; set; }

    public decimal? OldPrice { get; set; }

    public int? StockQuantity { get; set; }

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string? Cpu { get; set; }

    public string? Ram { get; set; }

    public string? HardDrive { get; set; }

    public string? Gpu { get; set; }

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
