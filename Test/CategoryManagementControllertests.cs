using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using LaptopStore.Models;
using LaptopStore.DTOs.CategoryDTOs;
using Moq;

namespace LaptopStore.Tests.Controllers
{
    public class CategoryManagementControllerTests : IDisposable
    {

        // Setup: InMemory DB + controller wired with TempData

        private readonly LaptopStoreDbContext _context;
        private readonly CategoryManagementController _controller;

        public CategoryManagementControllerTests()
        {
            var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // isolated DB per test class
                .Options;

            _context    = new LaptopStoreDbContext(options);
            _controller = new CategoryManagementController(_context);

            // TempData is required by AddNewCategory / UpdateCategory / DeleteCategory
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        public void Dispose() => _context.Dispose();

        // Seed helper

        private async Task<Category> SeedCategoryAsync(
            string name        = "Laptop Gaming",
            string description = "Cấu hình mạnh",
            bool   withProduct = false)
        {
            var category = new Category { Name = name, Description = description };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            if (withProduct)
            {
                // Minimal valid Product – adjust required fields to match your model
                _context.Products.Add(new Product
                {
                    Name        = "Test Product",
                    Sku         = Guid.NewGuid().ToString()[..20],
                    CategoryId  = category.Id,
                    Price       = 10_000_000,
                    Cpu         = "i5",
                    Ram         = "8GB",
                    HardDrive   = "256GB SSD",
                    ScreenSize  = "14 inch",
                    Weight      = "1.5 kg"
                });
                await _context.SaveChangesAsync();
            }

            return category;
        }

        //View Category List  –  Index()

        [Fact]
        public async Task Index_ReturnsViewResult()
        {
            // Act
            var result = _controller.Index();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Index_PassesAllCategoriesToView()
        {
            // Arrange
            await SeedCategoryAsync("Laptop Gaming");
            await SeedCategoryAsync("Laptop Văn Phòng");

            // Act
            var result   = _controller.Index() as ViewResult;
            var model    = result!.Model as List<Category>;

            // Assert
            Assert.NotNull(model);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Index_WhenNoCategories_ReturnsEmptyList()
        {
            // Act
            var result = _controller.Index() as ViewResult;
            var model  = result!.Model as List<Category>;

            // Assert
            Assert.NotNull(model);
            Assert.Empty(model);
        }

        // CategoryDetails()

        [Fact]
        public async Task CategoryDetails_ExistingId_ReturnsViewWithCategory()
        {
            // Arrange
            var category = await SeedCategoryAsync();

            // Act
            var result = _controller.CategoryDetails(category.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as Category;
            Assert.NotNull(model);
            Assert.Equal(category.Id, model.Id);
        }

        [Fact]
        public async Task CategoryDetails_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.CategoryDetails(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        //Add Category  –  GET AddNewCategory()

        [Fact]
        public void AddNewCategory_Get_ReturnsView()
        {
            // Act
            var result = _controller.AddNewCategory();

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        // US-6  Add Category  –  POST AddNewCategory()

        [Fact]
        public async Task AddNewCategory_Post_ValidRequest_SavesToDatabaseAndRedirects()
        {
            // Arrange
            var request = new AddCategoryRequest
            {
                Name        = "MacBook",
                Description = "Sản phẩm Apple"
            };

            // Act
            var result = _controller.AddNewCategory(request);

            // Assert – redirects to Index
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Assert – data persisted
            var saved = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "MacBook");
            Assert.NotNull(saved);
        }

        [Fact]
        public async Task AddNewCategory_Post_ValidRequest_SetsSuccessTempData()
        {
            // Arrange
            var request = new AddCategoryRequest { Name = "Ultrabook", Description = "Siêu mỏng" };

            // Act
            _controller.AddNewCategory(request);

            // Assert
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        [Fact]
        public async Task AddNewCategory_Post_DuplicateName_ReturnsViewWithModelError()
        {
            // Arrange – existing category
            await SeedCategoryAsync("Laptop Gaming");

            var request = new AddCategoryRequest
            {
                Name        = "laptop gaming", // case-insensitive duplicate
                Description = "Another description"
            };

            // Act
            var result = _controller.AddNewCategory(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey("Name"));
        }

        [Fact]
        public async Task AddNewCategory_Post_DuplicateName_DoesNotSaveToDatabase()
        {
            // Arrange
            await SeedCategoryAsync("Laptop Gaming");
            var countBefore = await _context.Categories.CountAsync();

            var request = new AddCategoryRequest { Name = "LAPTOP GAMING" };

            // Act
            _controller.AddNewCategory(request);

            // Assert
            Assert.Equal(countBefore, await _context.Categories.CountAsync());
        }

        [Fact]
        public void AddNewCategory_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");
            var request = new AddCategoryRequest { Name = "" };

            // Act
            var result = _controller.AddNewCategory(request);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task AddNewCategory_Post_TrimsWhitespace_BeforeSaving()
        {
            // Arrange
            var request = new AddCategoryRequest { Name = "  Laptop Gaming  " };

            // Act
            _controller.AddNewCategory(request);

            // Assert
            var saved = await _context.Categories.FirstOrDefaultAsync();
            Assert.Equal("Laptop Gaming", saved!.Name);
        }

        // US-7  Update Category  –  GET UpdateCategory()

        [Fact]
        public async Task UpdateCategory_Get_ExistingId_ReturnsViewWithModel()
        {
            // Arrange
            var category = await SeedCategoryAsync("Old Name", "Old Desc");

            // Act
            var result = _controller.UpdateCategory(category.Id) as ViewResult;

            // Assert
            Assert.NotNull(result);
            var model = result.Model as UpdateCategoryRequest;
            Assert.NotNull(model);
            Assert.Equal(category.Id,          model.Id);
            Assert.Equal("Old Name",           model.Name);
            Assert.Equal("Old Desc",           model.Description);
        }

        [Fact]
        public async Task UpdateCategory_Get_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.UpdateCategory(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        //Update Category  –  POST UpdateCategory()

        [Fact]
        public async Task UpdateCategory_Post_ValidRequest_UpdatesDatabaseAndRedirects()
        {
            // Arrange
            var category = await SeedCategoryAsync("Old Name");
            var request  = new UpdateCategoryRequest
            {
                Id          = category.Id,
                Name        = "New Name",
                Description = "New Desc"
            };

            // Act
            var result = _controller.UpdateCategory(request);

            // Assert – redirects to CategoryDetails
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CategoryDetails", redirect.ActionName);

            // Assert – DB updated
            _context.ChangeTracker.Clear(); // clear EF cache to re-read from DB
            var updated = await _context.Categories.FindAsync(category.Id);
            Assert.Equal("New Name", updated!.Name);
            Assert.Equal("New Desc", updated.Description);
        }

        [Fact]
        public async Task UpdateCategory_Post_ValidRequest_SetsSuccessTempData()
        {
            // Arrange
            var category = await SeedCategoryAsync("Cat A");
            var request  = new UpdateCategoryRequest { Id = category.Id, Name = "Cat A Updated" };

            // Act
            _controller.UpdateCategory(request);

            // Assert
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        [Fact]
        public async Task UpdateCategory_Post_DuplicateNameOnOtherCategory_ReturnsViewWithModelError()
        {
            // Arrange
            await SeedCategoryAsync("Existing Category");
            var target  = await SeedCategoryAsync("Target Category");

            var request = new UpdateCategoryRequest
            {
                Id   = target.Id,
                Name = "existing category" // duplicate (case-insensitive) of another record
            };

            // Act
            var result = _controller.UpdateCategory(request) as ViewResult;

            // Assert
            Assert.NotNull(result);
            Assert.True(_controller.ModelState.ContainsKey("Name"));
        }

        [Fact]
        public async Task UpdateCategory_Post_SameNameSameCategory_DoesNotTriggerDuplicateError()
        {
            // Arrange – updating a category with its own existing name should be allowed
            var category = await SeedCategoryAsync("Laptop Gaming");
            var request  = new UpdateCategoryRequest
            {
                Id          = category.Id,
                Name        = "Laptop Gaming", // same name, same Id → no conflict
                Description = "Updated description"
            };

            // Act
            var result = _controller.UpdateCategory(request);

            // Assert – should redirect, not return view with error
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task UpdateCategory_Post_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var request = new UpdateCategoryRequest { Id = 9999, Name = "Ghost" };

            // Act
            var result = _controller.UpdateCategory(request);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void UpdateCategory_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            _controller.ModelState.AddModelError("Name", "Required");
            var request = new UpdateCategoryRequest { Id = 1, Name = "" };

            // Act
            var result = _controller.UpdateCategory(request);

            // Assert
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task UpdateCategory_Post_TrimsWhitespace_BeforeSaving()
        {
            // Arrange
            var category = await SeedCategoryAsync("OldName");
            var request  = new UpdateCategoryRequest { Id = category.Id, Name = "  NewName  " };

            // Act
            _controller.UpdateCategory(request);

            // Assert
            _context.ChangeTracker.Clear();
            var updated = await _context.Categories.FindAsync(category.Id);
            Assert.Equal("NewName", updated!.Name);
        }

        //Delete Category  –  POST DeleteCategory()

        [Fact]
        public async Task DeleteCategory_ExistingCategoryNoProducts_DeletesAndRedirectsToIndex()
        {
            // Arrange
            var category = await SeedCategoryAsync("Empty Category");

            // Act
            var result = _controller.DeleteCategory(category.Id);

            // Assert – redirect
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);

            // Assert – removed from DB
            var deleted = await _context.Categories.FindAsync(category.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteCategory_ExistingCategoryNoProducts_SetsSuccessTempData()
        {
            // Arrange
            var category = await SeedCategoryAsync("Empty Category");

            // Act
            _controller.DeleteCategory(category.Id);

            // Assert
            Assert.True(_controller.TempData.ContainsKey("Success"));
        }

        [Fact]
        public async Task DeleteCategory_CategoryWithProducts_DoesNotDeleteCategory()
        {
            // Arrange
            var category = await SeedCategoryAsync("Used Category", withProduct: true);

            // Act
            _controller.DeleteCategory(category.Id);

            // Assert – still exists
            var stillExists = await _context.Categories.FindAsync(category.Id);
            Assert.NotNull(stillExists);
        }

        [Fact]
        public async Task DeleteCategory_CategoryWithProducts_SetsErrorTempData()
        {
            // Arrange
            var category = await SeedCategoryAsync("Used Category", withProduct: true);

            // Act
            _controller.DeleteCategory(category.Id);

            // Assert
            Assert.True(_controller.TempData.ContainsKey("Error"));
        }

        [Fact]
        public async Task DeleteCategory_CategoryWithProducts_RedirectsToCategoryDetails()
        {
            // Arrange
            var category = await SeedCategoryAsync("Used Category", withProduct: true);

            // Act
            var result = _controller.DeleteCategory(category.Id);

            // Assert
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("CategoryDetails", redirect.ActionName);
            Assert.Equal(category.Id, redirect.RouteValues!["id"]);
        }

        [Fact]
        public async Task DeleteCategory_NonExistentId_ReturnsNotFound()
        {
            // Act
            var result = _controller.DeleteCategory(9999);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteCategory_DecreasesCountByOne()
        {
            // Arrange
            var toDelete = await SeedCategoryAsync("ToDelete");
            await SeedCategoryAsync("KeepMe");
            var countBefore = await _context.Categories.CountAsync();

            // Act
            _controller.DeleteCategory(toDelete.Id);

            // Assert
            Assert.Equal(countBefore - 1, await _context.Categories.CountAsync());
        }
    }
}