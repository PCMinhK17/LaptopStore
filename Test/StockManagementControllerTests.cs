using LaptopStore.Controllers;
using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.DTOs.StockDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

public class StockManagementControllerTests
{
    private LaptopStoreDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new LaptopStoreDbContext(options);
    }
    private StockManagementController CreateController(LaptopStoreDbContext context)
    {
        var mockLogger = new Mock<ILogger<StockManagementController>>();
        return new StockManagementController(mockLogger.Object, context);
    }


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
                new StockInItemRequest { Product = new ProductResponse { Id = 1 }, Quantity = 5 },
                new StockInItemRequest { Product = new ProductResponse { Id = 2 }, Quantity = 10 },
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
        Assert.Equal("pending", savedReceipt.Status);

        // 3. Kiểm tra xem DB đã lưu ImportDetails chưa
        var savedDetail = context.ImportDetails.ToList();
        foreach (var detail in savedDetail)
        {
            Assert.NotNull(detail);
            Assert.Equal(request.Items.FirstOrDefault(i => i.Product.Id == detail.Id)?.Quantity, detail.RequestedQuantity);
        }

        // 4. Kiểm tra xem Notification đã được tạo chưa
        var notification = context.Notifications.FirstOrDefault();
        Assert.NotNull(notification);
        Assert.Equal(1, notification.UserId);
        Assert.Equal("receipt", notification.Type);
    }

    [Fact]
    public void StockDetails_OrderNotFound_ReturnsNotFound()
    {
        using var context = GetInMemoryDbContext();
        var controller = CreateController(context);

        var result = controller.StockDetails(999);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal("Không tìm thấy đơn hàng.", notFoundResult.Value);
    }
}