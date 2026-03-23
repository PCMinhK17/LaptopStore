using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using LaptopStore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using BCryptNet = BCrypt.Net.BCrypt;

namespace LaptopStore.Tests.Services
{
    public class AuthServiceRegisterTests
    {
        private readonly Mock<ILogger<AuthService>> _mockLogger;

        public AuthServiceRegisterTests()
        {
            _mockLogger = new Mock<ILogger<AuthService>>();
        }

        #region Helper Methods

        /// <summary>
        /// Tạo DbContext InMemory mới cho mỗi test (isolation)
        /// </summary>
        private LaptopStoreDbContext CreateContext(string? dbName = null)
        {
            var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
                .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
                .Options;
            return new LaptopStoreDbContext(options);
        }

        private AuthService CreateService(LaptopStoreDbContext context)
        {
            return new AuthService(context, _mockLogger.Object);
        }

        private static RegisterViewModel CreateValidModel()
        {
            return new RegisterViewModel
            {
                FullName = "Nguyễn Văn A",
                Email = "newuser@example.com",
                PhoneNumber = "0912345678",
                Address = "123 Đường ABC, Quận 1, TP.HCM",
                Password = "Test@1234",
                ConfirmPassword = "Test@1234"
            };
        }

        private static User CreateExistingUser(string email = "existing@example.com", string phone = "0987654321")
        {
            return new User
            {
                Email = email,
                FullName = "Existing User",
                Password = BCryptNet.HashPassword("OldPass@123"),
                PhoneNumber = phone,
                Address = "456 Đường XYZ",
                Role = "customer",
                Status = "active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        #endregion

        #region Success Tests

        [Fact]
        public async Task RegisterAsync_Success_CreatesUserInDatabase()
        {
            // Arrange
            using var context = CreateContext();
            var service = CreateService(context);
            var model = CreateValidModel();

            // Act
            var result = await service.RegisterAsync(model);

            // Assert
            Assert.True(result.Success);
            Assert.NotNull(result.User);
            Assert.Equal(model.Email, result.User!.Email);
            Assert.Equal(model.FullName, result.User.FullName);

            // Verify user exists in database
            var dbUser = await context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            Assert.NotNull(dbUser);
            Assert.Equal(model.Email, dbUser!.Email);
        }

        [Fact]
        public async Task RegisterAsync_SetsCorrectDefaults()
        {
            // Arrange
            using var context = CreateContext();
            var service = CreateService(context);
            var model = CreateValidModel();

            // Act
            var result = await service.RegisterAsync(model);

            // Assert
            Assert.True(result.Success);

            var user = result.User!;
            Assert.Equal("customer", user.Role);
            Assert.Equal("pending", user.Status); // pending vì cần xác thực email
            Assert.True(result.RequiresEmailVerification);
            Assert.NotNull(user.CreatedAt);
            Assert.NotNull(user.UpdatedAt);
            Assert.Equal("Account", result.RedirectController);
            Assert.Equal("VerificationSent", result.RedirectAction);
        }

        [Fact]
        public async Task RegisterAsync_PasswordIsHashed()
        {
            // Arrange
            using var context = CreateContext();
            var service = CreateService(context);
            var model = CreateValidModel();

            // Act
            var result = await service.RegisterAsync(model);

            // Assert
            Assert.True(result.Success);

            var user = result.User!;
            // Password KHÔNG được lưu dạng plain text
            Assert.NotEqual(model.Password, user.Password);

            // Password phải verify được bằng BCrypt
            Assert.True(BCryptNet.Verify(model.Password, user.Password));
        }

        [Fact]
        public async Task RegisterAsync_TrimsInputs()
        {
            // Arrange
            using var context = CreateContext();
            var service = CreateService(context);
            var model = new RegisterViewModel
            {
                FullName = "  Nguyễn Văn B  ",
                Email = "  spaceduser@example.com  ",
                PhoneNumber = "  0911223344  ",
                Address = "  789 Đường DEF  ",
                Password = "Test@1234",
                ConfirmPassword = "Test@1234"
            };

            // Act
            var result = await service.RegisterAsync(model);

            // Assert
            Assert.True(result.Success);

            var user = result.User!;
            Assert.Equal("Nguyễn Văn B", user.FullName);    // Trimmed
            Assert.Equal("spaceduser@example.com", user.Email);         // Trimmed
            Assert.Equal("0911223344", user.PhoneNumber);               // Trimmed
            Assert.Equal("789 Đường DEF", user.Address);                // Trimmed
        }

        #endregion

        #region Duplicate Tests

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsFail()
        {
            // Arrange
            using var context = CreateContext();
            // Thêm user có sẵn vào DB
            var existingUser = CreateExistingUser(email: "duplicate@example.com");
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var model = CreateValidModel();
            model.Email = "duplicate@example.com"; // Email trùng

            // Act
            var result = await service.RegisterAsync(model);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Email", result.ErrorMessage);
            Assert.Null(result.User);
        }

        [Fact]
        public async Task RegisterAsync_DuplicatePhone_ReturnsFail()
        {
            // Arrange
            using var context = CreateContext();
            // Thêm user có sẵn vào DB
            var existingUser = CreateExistingUser(phone: "0912345678");
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var model = CreateValidModel();
            model.PhoneNumber = "0912345678"; // Phone trùng

            // Act
            var result = await service.RegisterAsync(model);

            // Assert
            Assert.False(result.Success);
            Assert.Contains("Số điện thoại", result.ErrorMessage);
            Assert.Null(result.User);
        }

        #endregion
    }
}
