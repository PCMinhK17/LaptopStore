namespace LaptopStore.Models;

public partial class Brand
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string LogoUrl { get; set; } = null!;

    public string Origin { get; set; } = null!;

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
