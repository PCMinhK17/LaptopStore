using System.Security.Claims;
using LaptopStore.Controllers;
using LaptopStore.Models;
using LaptopStore.Models.ViewModels;
using LaptopStore.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace LaptopStore.Tests.Controllers
{
    public class AccountControllerRegisterTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<ILogger<AccountController>> _mockLogger;
        private readonly LaptopStoreDbContext _context;
        private readonly AccountController _controller;

        public AccountControllerRegisterTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockLogger = new Mock<ILogger<AccountController>>();

            // InMemory DB cho context
            var options = new DbContextOptionsBuilder<LaptopStoreDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new LaptopStoreDbContext(options);

            _controller = new AccountController(
                _mockAuthService.Object,
                _mockEmailService.Object,
                _context,
                _mockLogger.Object
            );

            // Setup HttpContext cơ bản
            SetupHttpContext(isAuthenticated: false);
        }

        #region Helper Methods

        private void SetupHttpContext(bool isAuthenticated)
        {
            var claims = new List<Claim>();
            if (isAuthenticated)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, "1"));
                claims.Add(new Claim(ClaimTypes.Email, "test@test.com"));
            }

            var identity = new ClaimsIdentity(
                claims,
                isAuthenticated ? "TestAuth" : null
            );
            var principal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext
            {
                User = principal
            };

            // Setup IAuthenticationService (required for SignInAsync)
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService
                .Setup(s => s.SignInAsync(
                    It.IsAny<HttpContext>(),
                    It.IsAny<string>(),
                    It.IsAny<ClaimsPrincipal>(),
                    It.IsAny<AuthenticationProperties>()))
                .Returns(Task.CompletedTask);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton(mockAuthenticationService.Object);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();

            // Setup Session
            httpContext.Session = new TestSession();

            // Setup TempData
            var tempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());
            _controller.TempData = tempData;

            // Setup ControllerContext
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            // Setup UrlHelper
            var mockUrlHelper = new Mock<IUrlHelper>();
            mockUrlHelper
                .Setup(u => u.Action(It.IsAny<UrlActionContext>()))
                .Returns("https://localhost/Account/VerifyEmail?token=test-token");
            mockUrlHelper
                .Setup(u => u.IsLocalUrl(It.IsAny<string>()))
                .Returns(false);
            _controller.Url = mockUrlHelper.Object;
        }

        private static RegisterViewModel CreateValidModel()
        {
            return new RegisterViewModel
            {
                FullName = "Nguyễn Văn A",
                Email = "test@example.com",
                PhoneNumber = "0912345678",
                Address = "123 Đường ABC, Quận 1, TP.HCM",
                Password = "Test@1234",
                ConfirmPassword = "Test@1234"
            };
        }

        #endregion

        #region GET Register Tests

        [Fact]
        public void Register_GET_ReturnsView_WhenNotAuthenticated()
        {
            // Arrange - already set up in constructor (not authenticated)

            // Act
            var result = _controller.Register();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsType<RegisterViewModel>(viewResult.Model);
        }

        [Fact]
        public void Register_GET_RedirectsToProduct_WhenAuthenticated()
        {
            // Arrange
            SetupHttpContext(isAuthenticated: true);

            // Act
            var result = _controller.Register();

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Product", redirectResult.ControllerName);
        }

        #endregion

        #region POST Register - Validation Tests

        [Fact]
        public async Task Register_POST_InvalidModel_ReturnsView()
        {
            // Arrange
            var model = new RegisterViewModel(); // empty model
            _controller.ModelState.AddModelError("Email", "Vui lòng nhập Email");

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Same(model, viewResult.Model);
        }

        #endregion

        #region POST Register - AuthService Error Tests

        [Fact]
        public async Task Register_POST_EmailExists_ShowsEmailError()
        {
            // Arrange
            var model = CreateValidModel();
            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Email này đã được sử dụng. Vui lòng sử dụng email khác."
                });

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(nameof(RegisterViewModel.Email)));
            Assert.Equal("error", _controller.TempData["ToastType"]);
        }

        [Fact]
        public async Task Register_POST_PhoneExists_ShowsPhoneError()
        {
            // Arrange
            var model = CreateValidModel();
            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Số điện thoại này đã được sử dụng. Vui lòng sử dụng số điện thoại khác."
                });

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(nameof(RegisterViewModel.PhoneNumber)));
            Assert.Equal("error", _controller.TempData["ToastType"]);
        }

        [Fact]
        public async Task Register_POST_GenericError_ShowsGenericError()
        {
            // Arrange
            var model = CreateValidModel();
            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau."
                });

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            // Generic error uses string.Empty key
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
            Assert.Equal("error", _controller.TempData["ToastType"]);
        }

        [Fact]
        public async Task Register_POST_NullUser_ShowsError()
        {
            // Arrange
            var model = CreateValidModel();
            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = true,
                    User = null // Success but no user
                });

            // Act
            var result = await _controller.Register(model);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.False(_controller.ModelState.IsValid);
            Assert.True(_controller.ModelState.ContainsKey(string.Empty));
        }

        #endregion

        #region POST Register - Success Tests

        [Fact]
        public async Task Register_POST_Success_RequiresVerification_SendsEmailAndRedirects()
        {
            // Arrange
            var model = CreateValidModel();
            var user = new User
            {
                Id = 1,
                Email = model.Email,
                FullName = model.FullName,
                Password = "hashed_password",
                Role = "customer",
                Status = "pending"
            };

            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = true,
                    User = user,
                    RequiresEmailVerification = true
                });

            _mockAuthService
                .Setup(s => s.GenerateEmailVerificationTokenAsync(user.Id))
                .ReturnsAsync("test-token-123");

            _mockEmailService
                .Setup(s => s.SendVerificationEmailAsync(
                    user.Email,
                    user.FullName!,
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Register(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VerificationSent", redirectResult.ActionName);

            // Verify email was sent
            _mockEmailService.Verify(
                s => s.SendVerificationEmailAsync(
                    user.Email,
                    user.FullName!,
                    It.IsAny<string>()),
                Times.Once);

            // Verify TempData was set
            Assert.Equal(user.Email, _controller.TempData["UserEmail"]);
            Assert.Equal(user.Id, _controller.TempData["UserId"]);
        }

        [Fact]
        public async Task Register_POST_Success_EmailSendFails_StillRedirectsWithWarning()
        {
            // Arrange
            var model = CreateValidModel();
            var user = new User
            {
                Id = 2,
                Email = model.Email,
                FullName = model.FullName,
                Password = "hashed_password",
                Role = "customer",
                Status = "pending"
            };

            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = true,
                    User = user,
                    RequiresEmailVerification = true
                });

            _mockAuthService
                .Setup(s => s.GenerateEmailVerificationTokenAsync(user.Id))
                .ReturnsAsync("test-token-123");

            // Email service throws exception
            _mockEmailService
                .Setup(s => s.SendVerificationEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP error"));

            // Act
            var result = await _controller.Register(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("VerificationSent", redirectResult.ActionName);

            // Should still set user info and show warning
            Assert.Equal(user.Email, _controller.TempData["UserEmail"]);
            Assert.Equal(user.Id, _controller.TempData["UserId"]);
            Assert.Equal("warning", _controller.TempData["ToastType"]);
        }

        [Fact]
        public async Task Register_POST_Success_NoVerificationRequired_RedirectsToProduct()
        {
            // Arrange
            var model = CreateValidModel();
            var user = new User
            {
                Id = 3,
                Email = model.Email,
                FullName = model.FullName,
                Password = "hashed_password",
                Role = "customer",
                Status = "active"
            };

            _mockAuthService
                .Setup(s => s.RegisterAsync(model))
                .ReturnsAsync(new AuthResult
                {
                    Success = true,
                    User = user,
                    RequiresEmailVerification = false,
                    RedirectAction = "Index",
                    RedirectController = "Product"
                });

            // Act
            var result = await _controller.Register(model);

            // Assert
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
            Assert.Equal("Product", redirectResult.ControllerName);

            Assert.Equal("success", _controller.TempData["ToastType"]);
        }

        #endregion
    }

    /// <summary>
    /// Simple in-memory ISession implementation for testing
    /// </summary>
    public class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public string Id => Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear() => _store.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

        public bool TryGetValue(string key, out byte[]? value) =>
            _store.TryGetValue(key, out value);
    }
}
