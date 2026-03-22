using LaptopStore.Controllers;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using Xunit;
// Nhớ giữ lại các using trỏ đến Controller và Model của bạn nhé (ví dụ: using LaptopStore.Controllers; using LaptopStore.Models;)

public class ProductSearchTest
{
    // Đổi sang dùng In-Memory Database giống hệt file Cart lúc nãy
    private LaptopStoreDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
            // Dùng Guid để mỗi Test Case có một DB riêng biệt, không bị lẫn lộn dữ liệu
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new LaptopStoreDbContext(options);
        return context;
    }

    // Hàm hỗ trợ tạo Product với ĐẦY ĐỦ các trường bắt buộc (Tránh lỗi DbUpdateException)
    private Product CreateValidProduct(int id, decimal price, int? brandId = null)
    {
        return new Product
        {
            Id = id,
            Price = price,
            BrandId = brandId,
            Name = $"Mock Laptop {id}",
            Cpu = "Intel Core i5",
            HardDrive = "512GB SSD",
            Ram = "8GB",
            ScreenSize = "15.6 inch",
            Sku = $"SKU-00{id}",
            Weight = "1,8", // Tùy chỉnh kiểu dữ liệu cho khớp với Model của bạn
           
            // Nếu model còn trường bắt buộc nào khác, bạn cứ điền thêm vào đây 1 lần là dùng cho mọi test case
        };
    }

    /// <summary>
    /// Test: Không filter → trả tất cả sản phẩm
    /// </summary>
    [Fact]
    public void Search_NoFilter_ReturnsAllProducts()
    {
        var context = GetDbContext();

        // Seed dữ liệu dùng hàm hỗ trợ
        context.Products.AddRange(
            CreateValidProduct(1, 5000000m),
            CreateValidProduct(2, 15000000m)
        );
        context.SaveChanges();

        var loggerMock = new Mock<ILogger<ProductController>>();
        var controller = new ProductController(loggerMock.Object, context);

        // Act
        var result = controller.SearchByCategoryAndBrand(null, null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<List<Product>>(viewResult.Model);

        Assert.Equal(2, model.Count);
    }

    /// <summary>
    /// Test: Filter theo Brand
    /// </summary>
    [Fact]
    public void Search_FilterByBrand_ReturnsCorrectProducts()
    {
        var context = GetDbContext();

        context.Products.AddRange(
            CreateValidProduct(1, 5000000m, brandId: 1),
            CreateValidProduct(2, 5000000m, brandId: 2)
        );
        context.SaveChanges();

        var loggerMock = new Mock<ILogger<ProductController>>();
        var controller = new ProductController(loggerMock.Object, context);

        // Act
        var result = controller.SearchByCategoryAndBrand(1, null, null);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = viewResult.Model as List<Product>;

        Assert.Single(model);
        Assert.Equal(1, model[0].BrandId);
    }

    /// <summary>
    /// Test: Filter theo PriceRange (case 1: < 10 triệu)
    /// </summary>
    [Fact]
    public void Search_FilterByPriceRange1_ReturnsCorrectProducts()
    {
        var context = GetDbContext();

        context.Products.AddRange(
            CreateValidProduct(1, 5000000m),   // < 10tr
            CreateValidProduct(2, 15000000m)   // > 10tr
        );
        context.SaveChanges();

        var loggerMock = new Mock<ILogger<ProductController>>();
        var controller = new ProductController(loggerMock.Object, context);

        // Act
        var result = controller.SearchByCategoryAndBrand(null, null, 1);

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = viewResult.Model as List<Product>;

        Assert.Single(model);
        Assert.True(model[0].Price < 10000000m);
    }
}