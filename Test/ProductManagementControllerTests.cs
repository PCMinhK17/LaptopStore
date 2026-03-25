using LaptopStore.Controllers;
using LaptopStore.DTOs.ProductDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class ProductManagementControllerTests
{
    // Hàm tạo DB Ảo
    private LaptopStoreDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new LaptopStoreDbContext(options);
    }

    // Hàm tạo Mock WebHostEnvironment
    private Mock<IWebHostEnvironment> GetMockEnvironment()
    {
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(m => m.WebRootPath).Returns("wwwroot"); // Giả lập thư mục wwwroot
        return mockEnv;
    }

    // Hàm tạo Controller với TempData để không bị lỗi NullReference
    private ProductManagementController CreateController(LaptopStoreDbContext context, IWebHostEnvironment env)
    {
        var controller = new ProductManagementController(env, context);

        // Cấu hình TempData giả lập
        var httpContext = new DefaultHttpContext();
        var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
        controller.TempData = tempData;

        return controller;
    }

    // Hàm tạo File ảnh giả lập
    private IFormFile GetMockImageFile()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024); // File size > 0
        mockFile.Setup(f => f.FileName).Returns("test-image.jpg");
        return mockFile.Object;
    }

    // Tạo Product hợp lệ mẫu
    private Product GetValidProduct()
    {
        return new Product
        {
            Name = "Laptop Test",
            Sku = "TESTSKU123",
            CategoryId = 2,
            BrandId = 1,
            Price = 1000m,
            Description = "Mô tả sản phẩm test",
            ShortDescription = "Mô tả ngắn gọn",
            Cpu = "Intel Core i7",
            Ram = "16GB",
            HardDrive = "512GB SSD",
            Gpu = "NVIDIA GTX 1650",
            ScreenSize = "15.6 inch",
            Weight = "2"
        };
    }

    // Tạo AddNewProductRequest hợp lệ mẫu
    private AddNewProductRequest GetValidAddNewProductRequest()
    {
        return new AddNewProductRequest
        {
            Name = "Laptop Test",
            Sku = "TESTSKU123",
            CategoryId = 2,
            BrandId = 1,
            Price = "1000",
            Description = "Mô tả sản phẩm test",
            ShortDescription = "Mô tả ngắn gọn",
            Cpu = "Intel Core i7",
            Ram = "16GB",
            HardDrive = "512GB SSD",
            Gpu = "NVIDIA GTX 1650",
            ScreenSize = "15.6 inch",
            Weight = "2",
            ImageFile = GetMockImageFile()
        };
    }

    [Fact]
    public async Task AddNewProduct_ValidData_SavesProductAndImage_RedirectsToIndex()
    {
        using var context = GetInMemoryDbContext();
        var mockEnv = GetMockEnvironment();
        var controller = CreateController(context, mockEnv.Object);

        var newProduct = GetValidAddNewProductRequest();

        var result = await controller.AddNewProduct(newProduct) as RedirectToActionResult;

        // Assert
        // 1. Kiểm tra chuyển hướng
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);

        // 2. Kiểm tra thông báo TempData
        Assert.Equal("Thêm sản phẩm mới thành công", controller.TempData["ToastMessage"]);
        Assert.Equal("success", controller.TempData["ToastType"]);

        var savedProduct = context.Products.FirstOrDefault();
        Assert.NotNull(savedProduct);
        Assert.Equal("Laptop Test", savedProduct.Name);
        Assert.Equal("TESTSKU123", savedProduct.Sku);
        Assert.Equal(1, savedProduct.BrandId);
        Assert.Equal(2, savedProduct.CategoryId);
        Assert.Equal(1000m, savedProduct.Price);
        Assert.Equal("Mô tả sản phẩm test", savedProduct.Description);
        Assert.Equal("Mô tả ngắn gọn", savedProduct.ShortDescription);
        Assert.Equal("Intel Core i7", savedProduct.Cpu);
        Assert.Equal("16GB", savedProduct.Ram);
        Assert.Equal("512GB SSD", savedProduct.HardDrive);
        Assert.Equal("NVIDIA GTX 1650", savedProduct.Gpu);
        Assert.Equal("15.6 inch", savedProduct.ScreenSize);
        Assert.Equal("2", savedProduct.Weight);

        var savedImage = context.ProductImages.FirstOrDefault();
        Assert.NotNull(savedImage);
        Assert.Equal(savedProduct.Id, savedImage.ProductId);
        Assert.True(savedImage.IsThumbnail);
    }

    [Fact]
    public async Task AddNewProduct_DuplicateSku_ReturnsViewWithError()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        // Tạo sẵn 1 sản phẩm có SKU là "SKU-999"
        var product = GetValidProduct();
        product.Sku = "SKU-999";
        context.Products.Add(product);

        await context.SaveChangesAsync();

        var controller = CreateController(context, GetMockEnvironment().Object);
        var newProduct = GetValidAddNewProductRequest();
        newProduct.Sku = "SKU-999"; // Nhập trùng SKU

        // Act
        var result = await controller.AddNewProduct(newProduct) as ViewResult;

        // Assert
        Assert.NotNull(result);
        Assert.False(controller.ModelState.IsValid); // ModelState phải bị false

        // Kiểm tra xem lỗi trả về có đúng key "Sku" không
        var skuError = controller.ModelState["Sku"].Errors.First().ErrorMessage;
        Assert.Equal("Mã SKU này đã được dùng bởi sản phẩm khác", skuError);
    }

    [Theory]
    [InlineData("1000", "1.5")] 
    [InlineData("1000000000", "1.5")] 
    [InlineData("15000000", "1")]
    [InlineData("15000000", "100")]
    public async Task AddNewProduct_InOfBoundsValues_RedirectsToIndex(string price, string weight)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = CreateController(context, GetMockEnvironment().Object);

        var newProduct = GetValidAddNewProductRequest();
        // Gán giá trị không hợp lệ
        newProduct.Price = price;
        newProduct.Weight = weight;
        // Act
        var result = await controller.AddNewProduct(newProduct) as RedirectToActionResult;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);

        // 2. Kiểm tra thông báo TempData
        Assert.Equal("Thêm sản phẩm mới thành công", controller.TempData["ToastMessage"]);
        Assert.Equal("success", controller.TempData["ToastType"]);
    }


    [Theory]
    [InlineData("999", "1.5", "Price", "Giá phải nằm trong khoảng 1.000 đến 1.000.000.000")] // Giá quá thấp
    [InlineData("1000000001", "1.5", "Price", "Giá phải nằm trong khoảng 1.000 đến 1.000.000.000")] // Giá quá cao
    [InlineData("15000000", "0.9", "Weight", "Cân nặng phải nằm trong khoảng 1 đến 100")] // Nhẹ quá
    [InlineData("15000000", "101", "Weight", "Cân nặng phải nằm trong khoảng 1 đến 100")] // Nặng quá
    public async Task AddNewProduct_OutOfBoundsValues_ReturnsViewWithErrors(string price, string weight, string errorKey, string expectedErrorMsg)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var controller = CreateController(context, GetMockEnvironment().Object);

        var newProduct = GetValidAddNewProductRequest();
        // Gán giá trị không hợp lệ
        newProduct.Price = price; 
        newProduct.Weight = weight;
        // Act
        var result = await controller.AddNewProduct(newProduct) as ViewResult;

        // Assert
        Assert.NotNull(result);
        var error = controller.ModelState[errorKey].Errors.First().ErrorMessage;
        Assert.Equal(expectedErrorMsg, error);

        // Kiểm tra TempData hiển thị toast lỗi
        Assert.Equal("Vui lòng kiểm tra lại thông tin.", controller.TempData["ToastMessage"]);
        Assert.Equal("error", controller.TempData["ToastType"]);
    }

    [Fact]
    public async Task AddNewProduct_ImageSaveThrowsException_SavesProductButNoImage_AndRedirects()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var mockEnv = GetMockEnvironment();
        var controller = CreateController(context, mockEnv.Object);

        // Tạo file ảnh giả, nhưng cố tình setup cho CopyToAsync quăng ra lỗi
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.FileName).Returns("test.jpg");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("Không có quyền lưu file"));

        var newProduct = GetValidAddNewProductRequest();
        newProduct.ImageFile = mockFile.Object; // Gán file ảnh giả có lỗi

        // Act
        var result = await controller.AddNewProduct(newProduct) as RedirectToActionResult;

        // Assert
        // Vẫn phải trả về Index bình thường (vì đã catch exception)
        Assert.NotNull(result);
        Assert.Equal("Index", result.ActionName);

        // Sản phẩm vẫn được lưu
        Assert.Equal(1, context.Products.Count());

        // Nhưng bảng ProductImages sẽ không có dữ liệu (vì crash trước khi Add)
        Assert.Empty(context.ProductImages);
    }
}