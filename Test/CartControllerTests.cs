using LaptopStore.Controllers;
using LaptopStore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Xunit;
// Thêm using thư mục Models và Controllers của bạn vào đây

public class CartControllerTests
{
    // Hàm hỗ trợ tạo DbContext giả lập (In-Memory)
    private LaptopStoreDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new LaptopStoreDbContext(options);
    }

    // Hàm hỗ trợ tạo Controller và giả lập User đăng nhập
    private CartController CreateController(LaptopStoreDbContext context, string userId = null)
    {
        var controller = new CartController(context);

        // Giả lập HttpContext cho User
        var httpContext = new DefaultHttpContext();
        // Giả lập HttpContext cho User
       
        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId),
            
            // BỔ SUNG THÊM CÁC DÒNG NÀY ĐỂ "BAO TRÚNG" HÀM GetUserId() CỦA BẠN:
            new Claim(ClaimTypes.Name, userId),
            new Claim("UserId", userId),
            new Claim("id", userId),
            new Claim("AccountId", userId)
        };
            var identity = new ClaimsIdentity(claims, "mock");
            httpContext.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        return controller;
    }

    [Fact]
    public void CartView_UserNotLoggedIn_ReturnsEmptyCart()
    {
        // Arrange (Setup Test Case 01)
        using var context = GetInMemoryDbContext();
        var controller = CreateController(context, userId: null); // Không truyền userId

        // Act
        var result = controller.CartView() as ViewResult;
        var model = result?.Model as Cart;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.NotNull(model.CartItems);
        Assert.Empty(model.CartItems); // Đảm bảo giỏ hàng rỗng
    }

    [Fact]
    public void CartView_UserLoggedIn_NoExistingCart_ReturnsEmptyCart()
    {
        // Arrange (Setup Test Case 02)
        using var context = GetInMemoryDbContext();
        // Không thêm bất kỳ Cart nào vào DB giả lập
        var controller = CreateController(context, userId: "1");

        // Act
        var result = controller.CartView() as ViewResult;
        var model = result?.Model as Cart;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.NotNull(model.CartItems);
        Assert.Empty(model.CartItems);
    }

    [Fact]
    public void CartView_UserLoggedIn_HasExistingCart_ReturnsCartWithItems()
    {
        // Arrange (Setup Test Case 03)
        using var context = GetInMemoryDbContext();
        var userId = 2;

        // Tạo dữ liệu giả với ĐẦY ĐỦ Khóa chính (Id) và các object liên kết
        var mockCart = new Cart
        {
            Id = 1, // Ép cứng Id
            UserId = userId,
            CartItems = new List<CartItem>
    {
        new CartItem
{
    Id = 1,
    CartId = 1,
    ProductId = 1,
    Product = new Product
    {
        Id = 1,
        Name = "Laptop ABC",
        
        // BỔ SUNG CÁC TRƯỜNG BẮT BUỘC Ở ĐÂY:
        Cpu = "Intel Core i5",
        HardDrive = "512GB SSD",
        Ram = "8GB",
        ScreenSize = "15.6 inch",
        Sku = "LAPTOP-ABC-001",
        Weight = "1,8",
        // (Nếu nó còn báo thiếu trường nào như Price, Description... thì bạn cứ thêm đại 1 giá trị vào nhé)

        ProductImages = new List<ProductImage>(),
       Brand = new Brand
{
    Id = 1,
    Name = "Mock Brand",
    LogoUrl = "default-logo.png", // Bổ sung
    Origin = "USA"                // Bổ sung
}
    }
}
    }
        };

        context.Carts.Add(mockCart);
        context.SaveChanges();

        // ... (Phần Khởi tạo controller và Act, Assert bên dưới giữ nguyên)

        // Khởi tạo controller với userId = 2
        var controller = CreateController(context, userId: userId.ToString());

        // Act
        var result = controller.CartView() as ViewResult;
        var model = result?.Model as Cart;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(model);
        Assert.Equal(userId, model.UserId);
        Assert.Single(model.CartItems); // Đảm bảo lấy ra đúng 1 sản phẩm đã add ở trên
        Assert.Equal("Laptop ABC", model.CartItems.First().Product.Name);
    }


}