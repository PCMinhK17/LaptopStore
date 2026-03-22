using LaptopStore.Controllers;
using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.DTOs.StockDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class StockManagementControllerTests
{
    // 1. Hàm tạo In-Memory Database (Tương tự CartController)
    private LaptopStoreDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new LaptopStoreDbContext(options);
    }

    // 2. Hàm tạo Controller với Logger giả (Mock Logger)
    private StockManagementController CreateController(LaptopStoreDbContext context)
    {
        var mockLogger = new Mock<ILogger<StockManagementController>>();
        return new StockManagementController(mockLogger.Object, context);
    }


    // ==========================================
    // TEST CASE 1: Kiểm tra ConfirmAddNewStockInOrder (Lưu Data & Chuyển hướng)
    // ==========================================
    [Fact]
    public void ConfirmAddNewStockInOrder_ValidData_SavesToDbAndRedirects()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = CreateController(context);

        var request = new StockInOrderRequest
        {
            SupplierName = "NCC Laptop Asus",
            StaffId = 1,
            Items = new List<StockInItemRequest>
            {
                new StockInItemRequest { Product = new ProductResponse { Id = 101 }, Quantity = 5 }
            },
        };

        // Act
        var result = controller.ConfirmAddNewStockInOrder(request) as RedirectToActionResult;

        // Assert
        // 1. Kiểm tra kết quả trả về có phải là chuyển hướng đến "StockDetails" không
        Assert.NotNull(result);
        Assert.Equal("StockDetails", result.ActionName);

        // 2. Kiểm tra xem DB đã lưu ImportReceipt chưa
        var savedReceipt = context.ImportReceipts.FirstOrDefault();
        Assert.NotNull(savedReceipt);
        Assert.Equal("NCC Laptop Asus", savedReceipt.SupplierName);

        // 3. Kiểm tra xem DB đã lưu ImportDetails chưa
        var savedDetail = context.ImportDetails.FirstOrDefault();
        Assert.NotNull(savedDetail);
        Assert.Equal(101, savedDetail.ProductId);
        Assert.Equal(5, savedDetail.RequestedQuantity);

        // 4. Kiểm tra xem Notification đã được tạo chưa
        var notification = context.Notifications.FirstOrDefault();
        Assert.NotNull(notification);
        Assert.Equal(1, notification.UserId);
        Assert.Equal("receipt", notification.Type);
    }

    // ==========================================
    // TEST CASE 2: Kiểm tra StockDetails (Không tìm thấy ID)
    // ==========================================
    [Fact]
    public void StockDetails_OrderNotFound_ReturnsNotFound()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = CreateController(context); // DB trống

        // Act
        var result = controller.StockDetails(999); // ID không tồn tại

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Không tìm thấy đơn hàng.", notFoundResult.Value);
    }
}