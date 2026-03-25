using LaptopStore.DTOs.BrandDTOs;
using LaptopStore.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LaptopStore.Tests.Controllers
{
    public class BrandManagementControllerTests : IDisposable
    {

        // Setup: In-memory DB + Controller

        private readonly LaptopStoreDbContext _context;
        private readonly BrandManagementController _controller;

        public BrandManagementControllerTests()
        {
            var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new LaptopStoreDbContext(options);
            _controller = new BrandManagementController(_context);

            // Cần TempData để controller không bị null reference
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>()   // hoặc dùng TempDataDictionaryFactory nếu không có Moq
            );
        }

        public void Dispose() => _context.Dispose();

        // Helper: tạo brand nhanh

        private Brand SeedBrand(string name = "Dell", string origin = "USA", bool withProduct = false, string logoUrl = "")
        {
            var brand = new Brand { Name = name, Origin = origin, LogoUrl = logoUrl };
            _context.Brands.Add(brand);
            _context.SaveChanges();

            if (withProduct)
            {
                var product = new Product { Name = "Laptop Test", BrandId = brand.Id, Cpu = "test", HardDrive = "test", Ram = "test", ScreenSize = "test", Sku = Guid.NewGuid().ToString(), Weight = "10" };
                _context.Products.Add(product);
                _context.SaveChanges();
            }

            // Reload để include Products
            _context.Entry(brand).Collection(b => b.Products).Load();
            return brand;
        }

        //  Index – View Brand List

        [Fact]
        public void Index_WhenNoBrands_ReturnsViewWithEmptyList()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as List<BrandResponse>;
            Assert.NotNull(model);
            Assert.Empty(model);
        }

        [Fact]
        public void Index_WhenBrandsExist_ReturnsCorrectCount()
        {
            // Arrange
            SeedBrand("Dell");
            SeedBrand("HP");
            SeedBrand("Lenovo");

            // Act
            var result = _controller.Index() as ViewResult;
            var model = result.Model as List<BrandResponse>;

            // Assert
            Assert.Equal(3, model.Count);
        }

        [Fact]
        public void Index_ReturnsBrandResponseWithCorrectFields()
        {
            // Arrange
            SeedBrand("Apple", "USA", withProduct: true);

            // Act
            var result = _controller.Index() as ViewResult;
            var model  = result.Model as List<BrandResponse>;
            var brand  = model.First();

            // Assert
            Assert.Equal("Apple", brand.Name);
            Assert.Equal("USA",   brand.Origin);
            Assert.Equal(1,       brand.TotalProducts);
        }

        [Fact]
        public void Index_ReturnsCorrectView()
        {
            // Act
            var result = _controller.Index() as ViewResult;

            // Assert
            Assert.Equal("~/Views/Manager/BrandManagement.cshtml", result.ViewName);
        }

        //  AddNewBrand GET

        [Fact]
        public void AddNewBrand_Get_ReturnsCorrectView()
        {
            // Act
            var result = _controller.AddNewBrand() as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("~/Views/Manager/AddNewBrand.cshtml", result.ViewName);
        }

        //  AddNewBrand POST

        [Fact]
        public void AddNewBrand_Post_ValidRequest_SavesBrandToDatabase()
        {
            // Arrange
            var request = new AddNewBrandRequest { Name = "Samsung", Origin = "Korea", LogoFile = null };

            // Act
            _controller.AddNewBrand(request);

            // Assert
            var saved = _context.Brands.FirstOrDefault(b => b.Name == "Samsung");
            Assert.NotNull(saved);
            Assert.Equal("Korea", saved.Origin);
        }

        [Fact]
        public void AddNewBrand_Post_ValidRequest_RedirectsToIndex()
        {
            // Arrange
            var request = new AddNewBrandRequest { Name = "Asus", Origin = "Taiwan" };

            // Act
            var result = _controller.AddNewBrand(request) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public void AddNewBrand_Post_ValidRequest_SetsTempDataSuccess()
        {
            // Arrange
            var request = new AddNewBrandRequest { Name = "Acer", Origin = "Taiwan" };

            // Act
            _controller.AddNewBrand(request);

            // Assert
            Assert.Equal("Thêm thương hiệu thành công.", _controller.TempData["Success"]);
        }

        [Fact]
        public void AddNewBrand_Post_DuplicateName_DoesNotSave()
        {
            // Arrange
            SeedBrand("Dell");
            var request = new AddNewBrandRequest { Name = "Dell", Origin = "USA" }; // trùng tên

            // Act
            _controller.AddNewBrand(request);

            // Assert – vẫn chỉ có 1 brand tên Dell
            Assert.Equal(1, _context.Brands.Count(b => b.Name == "Dell"));
        }

        [Fact]
        public void AddNewBrand_Post_DuplicateName_ReturnsViewWithModelError()
        {
            // Arrange
            SeedBrand("Dell");
            var request = new AddNewBrandRequest { Name = "dell", Origin = "USA" }; // case-insensitive

            // Act
            var result = _controller.AddNewBrand(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ViewData.ModelState.ContainsKey("Name"));
        }

        [Fact]
        public void AddNewBrand_Post_InvalidModel_ReturnsViewWithoutSaving()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");
            var request = new AddNewBrandRequest { Name = "" };

            // Act
            var result = _controller.AddNewBrand(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0, _context.Brands.Count());
        }

        [Fact]
        public void AddNewBrand_Post_TrimsNameBeforeSaving()
        {
            // Arrange
            var request = new AddNewBrandRequest { Name = "  MSI  ", Origin = "Taiwan"};

            // Act
            _controller.AddNewBrand(request);

            // Assert
            var saved = _context.Brands.First();
            Assert.Equal("MSI", saved.Name); // tên đã được trim
        }

        //  BrandDetails

        [Fact]
        public void BrandDetails_ExistingId_ReturnsViewWithBrand()
        {
            // Arrange
            var brand = SeedBrand("HP");

            // Act
            var result = _controller.BrandDetails(brand.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as Brand;
            Assert.Equal("HP", model.Name);
        }

        [Fact]
        public void BrandDetails_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.BrandDetails(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        //  UpdateBrand GET

        [Fact]
        public void UpdateBrand_Get_ExistingId_ReturnsViewWithModel()
        {
            // Arrange
            var brand = SeedBrand("Lenovo", "China");

            // Act
            var result = _controller.UpdateBrand(brand.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as UpdateBrandRequest;
            Assert.Equal(brand.Id, model.Id);
            Assert.Equal("Lenovo",  model.Name);
            Assert.Equal("China",   model.Origin);
        }

        [Fact]
        public void UpdateBrand_Get_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.UpdateBrand(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        //  UpdateBrand POST

        [Fact]
        public void UpdateBrand_Post_ValidRequest_UpdatesNameAndOriginInDatabase()
        {
            // Arrange
            var brand = SeedBrand("OldName", "OldOrigin");
            var request = new UpdateBrandRequest
            {
                Id     = brand.Id,
                Name   = "NewName",
                Origin = "NewOrigin"
            };

            // Act
            _controller.UpdateBrand(request);

            // Assert
            var updated = _context.Brands.Find(brand.Id);
            Assert.Equal("NewName",   updated.Name);
            Assert.Equal("NewOrigin", updated.Origin);
        }

        [Fact]
        public void UpdateBrand_Post_ValidRequest_RedirectsToBrandDetails()
        {
            // Arrange
            var brand = SeedBrand("Asus");
            var request = new UpdateBrandRequest { Id = brand.Id, Name = "Asus Updated", Origin = "Taiwan" };

            // Act
            var result = _controller.UpdateBrand(request) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BrandDetails", result.ActionName);
            Assert.Equal(brand.Id, result.RouteValues["id"]);
        }

        [Fact]
        public void UpdateBrand_Post_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateBrandRequest { Id = 9999, Name = "Ghost", Origin = "Nowhere" };

            // Act
            var result = _controller.UpdateBrand(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void UpdateBrand_Post_DuplicateNameWithOtherBrand_ReturnsViewWithModelError()
        {
            // Arrange
            SeedBrand("Dell");
            var brand2 = SeedBrand("HP");

            // Đổi HP thành Dell (tên đã có)
            var request = new UpdateBrandRequest { Id = brand2.Id, Name = "Dell", Origin = "USA" };

            // Act
            var result = _controller.UpdateBrand(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(result.ViewData.ModelState.ContainsKey("Name"));
        }

        [Fact]
        public void UpdateBrand_Post_SameNameSameBrand_DoesNotTreatAsDuplicate()
        {
            // Arrange – cập nhật brand bằng chính tên cũ của nó (không phải duplicate)
            var brand = SeedBrand("Lenovo", "OldOrigin");
            var request = new UpdateBrandRequest { Id = brand.Id, Name = "Lenovo", Origin = "NewOrigin" };

            // Act
            var result = _controller.UpdateBrand(request);

            // Assert – không bị lỗi, vẫn redirect
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public void UpdateBrand_Post_InvalidModel_ReturnsViewWithoutSaving()
        {
            // Arrange
            var brand = SeedBrand("ValidBrand");
            _controller.ModelState.AddModelError("Name", "Required");
            var request = new UpdateBrandRequest { Id = brand.Id, Name = "" };

            // Act
            var result = _controller.UpdateBrand(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var unchanged = _context.Brands.Find(brand.Id);
            Assert.Equal("ValidBrand", unchanged.Name); // không bị sửa
        }

        //  DeleteBrand GET

        [Fact]
        public void DeleteBrand_Get_ExistingId_ReturnsViewWithBrand()
        {
            // Arrange
            var brand = SeedBrand("Acer");

            // Act
            var result = _controller.DeleteBrand(brand.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as Brand;
            Assert.Equal("Acer", model.Name);
        }

        [Fact]
        public void DeleteBrand_Get_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.DeleteBrand(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        //  DeleteBrandConfirmed POST

        [Fact]
        public void DeleteBrandConfirmed_BrandWithNoProducts_RemovesBrandFromDatabase()
        {
            // Arrange
            var brand = SeedBrand("Unused Brand");

            // Act
            _controller.DeleteBrandConfirmed(brand.Id);

            // Assert
            var deleted = _context.Brands.Find(brand.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public void DeleteBrandConfirmed_BrandWithNoProducts_RedirectsToIndex()
        {
            // Arrange
            var brand = SeedBrand("Unused Brand");

            // Act
            var result = _controller.DeleteBrandConfirmed(brand.Id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
        }

        [Fact]
        public void DeleteBrandConfirmed_BrandWithNoProducts_SetsTempDataSuccess()
        {
            // Arrange
            var brand = SeedBrand("Unused Brand");

            // Act
            _controller.DeleteBrandConfirmed(brand.Id);

            // Assert
            Assert.Equal("Xóa thương hiệu thành công.", _controller.TempData["Success"]);
        }

        [Fact]
        public void DeleteBrandConfirmed_BrandWithProducts_DoesNotDelete()
        {
            // Arrange
            var brand = SeedBrand("Used Brand", withProduct: true);

            // Act
            _controller.DeleteBrandConfirmed(brand.Id);

            // Assert
            var stillExists = _context.Brands.Find(brand.Id);
            Assert.NotNull(stillExists);
        }

        [Fact]
        public void DeleteBrandConfirmed_BrandWithProducts_RedirectsToBrandDetails()
        {
            // Arrange
            var brand = SeedBrand("Used Brand", withProduct: true);

            // Act
            var result = _controller.DeleteBrandConfirmed(brand.Id) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("BrandDetails", result.ActionName);
            Assert.Equal(brand.Id, result.RouteValues["id"]);
        }

        [Fact]
        public void DeleteBrandConfirmed_BrandWithProducts_SetsTempDataError()
        {
            // Arrange
            var brand = SeedBrand("Used Brand", withProduct: true);

            // Act
            _controller.DeleteBrandConfirmed(brand.Id);

            // Assert
            Assert.Equal(
                "Không thể xóa thương hiệu vì vẫn còn sản phẩm thuộc thương hiệu này.",
                _controller.TempData["Error"]
            );
        }

        [Fact]
        public void DeleteBrandConfirmed_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.DeleteBrandConfirmed(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DeleteBrandConfirmed_DecreasesTotalBrandCount()
        {
            // Arrange
            var toDelete = SeedBrand("Delete Me");
            SeedBrand("Keep Me");

            // Act
            _controller.DeleteBrandConfirmed(toDelete.Id);

            // Assert
            Assert.Equal(1, _context.Brands.Count());
        }
    }
}