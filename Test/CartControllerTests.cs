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
        if (!string.IsNullOrEmpty(userId))
        {
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
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

        // Tạo dữ liệu giả đưa vào DB In-Memory
        var mockCart = new Cart
        {
            UserId = userId,
            CartItems = new List<CartItem>
            {
                new CartItem { Product = new Product { Name = "Laptop ABC" } }
            }
        };
        context.Carts.Add(mockCart);
        context.SaveChanges();

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