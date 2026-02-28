namespace LaptopStore.DTOs.BrandDTOs;

public class BrandResponse
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? LogoUrl { get; set; }

    public string? Origin { get; set; }

    public int TotalProducts { get; set; }
}
