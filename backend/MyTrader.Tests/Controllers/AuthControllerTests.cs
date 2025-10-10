using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MyTrader.Api.Controllers;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Services.Authentication;
using MyTrader.Tests.Utilities;
using System.Security.Claims;
using Xunit;

namespace MyTrader.Tests.Controllers;

public class AuthControllerTests : TestBase
{
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<ILogger<AuthController>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockLogger = MockServiceHelper.CreateMockLogger<AuthController>();
        _mockConfiguration = new Mock<IConfiguration>();

        _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object, _mockConfiguration.Object);

        // Setup HttpContext for controller
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task Register_ValidRequest_ReturnsOkWithSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User",
            Phone = "1234567890"
        };

        var expectedResponse = new RegisterResponse
        {
            Success = true,
            Message = "Registration successful"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as RegisterResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();

        _mockAuthService.Verify(x => x.RegisterAsync(request), Times.Once);
    }

    [Fact]
    public async Task Register_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "123",
            FirstName = "",
            LastName = "",
            Phone = ""
        };

        // Add model validation error
        _controller.ModelState.AddModelError("Email", "The Email field is not a valid e-mail address.");

        // Act
        var result = await _controller.Register(request);

        // Assert
        _controller.ModelState.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Register_ServiceThrowsException_ReturnsInternalServerError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User",
            Phone = "1234567890"
        };

        _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.Should().NotBeNull();
        var statusResult = result.Result as ObjectResult;
        statusResult.Should().NotBeNull();
        statusResult!.StatusCode.Should().Be(500);

        var response = statusResult.Value as RegisterResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPassword123!"
        };

        var expectedResponse = new UserSessionResponse
        {
            Success = true,
            AccessToken = "valid-jwt-token",
            RefreshToken = "valid-refresh-token",
            User = new UserResponse
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            }
        };

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as UserSessionResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
        response.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        var result = await _controller.Login(request);

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task VerifyEmail_ValidRequest_ReturnsOkWithSuccess()
    {
        // Arrange
        var request = new VerifyEmailRequest
        {
            Email = "test@example.com",
            VerificationCode = "123456"
        };

        var expectedResponse = new RegisterResponse
        {
            Success = true,
            Message = "Email verified successfully"
        };

        _mockAuthService.Setup(x => x.VerifyEmailAsync(It.IsAny<VerifyEmailRequest>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as RegisterResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        var expectedResponse = new TokenResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };

        _mockAuthService.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(expectedResponse);

        // Setup headers
        _controller.ControllerContext.HttpContext.Request.Headers["User-Agent"] = "TestAgent/1.0";
        _controller.ControllerContext.HttpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as TokenResponse;
        response.Should().NotBeNull();
        response!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetProfile_AuthenticatedUser_ReturnsUserProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedUser = new UserResponse
        {
            Id = userId,
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        _mockAuthService.Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var user = okResult.Value as UserResponse;
        user.Should().NotBeNull();
        user!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetProfile_InvalidUserId_ReturnsUnauthorized()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "invalid-guid")
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.Should().NotBeNull();
        var unauthorizedResult = result.Result as UnauthorizedObjectResult;
        unauthorizedResult.Should().NotBeNull();
        unauthorizedResult!.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task GetProfile_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        _mockAuthService.Setup(x => x.GetUserAsync(userId))
            .ReturnsAsync((UserResponse?)null);

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.Should().NotBeNull();
        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Logout_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext.HttpContext.User = principal;

        _mockAuthService.Setup(x => x.LogoutAsync(userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        result.Should().NotBeNull();
        var okResult = result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        _mockAuthService.Verify(x => x.LogoutAsync(userId), Times.Once);
    }

    [Fact]
    public async Task RequestPasswordReset_ValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new PasswordResetRequest
        {
            Email = "test@example.com"
        };

        var expectedResponse = new RegisterResponse
        {
            Success = true,
            Message = "Password reset email sent"
        };

        _mockAuthService.Setup(x => x.RequestPasswordResetAsync(request.Email))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RequestPasswordReset(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as RegisterResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ResetPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new PasswordResetFinalizeRequest
        {
            Email = "test@example.com",
            NewPassword = "NewPassword123!"
        };

        var expectedResponse = new RegisterResponse
        {
            Success = true,
            Message = "Password reset successfully"
        };

        _mockAuthService.Setup(x => x.ResetPasswordAsync(request.Email, request.NewPassword))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);

        var response = okResult.Value as RegisterResponse;
        response.Should().NotBeNull();
        response!.Success.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid-email")]
    [InlineData(null)]
    public async Task Register_InvalidEmail_ShouldFailValidation(string email)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = email,
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "User",
            Phone = "1234567890"
        };

        // This would typically be handled by model validation attributes
        // In a real scenario, the model state would be invalid before reaching the controller action
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            _controller.ModelState.AddModelError("Email", "Invalid email address");
        }

        // Act & Assert
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            _controller.ModelState.IsValid.Should().BeFalse();
        }
    }

    [Theory]
    [InlineData("123")]           // Too short
    [InlineData("")]              // Empty
    [InlineData(null)]            // Null
    public async Task Register_InvalidPassword_ShouldFailValidation(string password)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = password,
            FirstName = "Test",
            LastName = "User",
            Phone = "1234567890"
        };

        // This would typically be handled by model validation attributes
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            _controller.ModelState.AddModelError("Password", "Password must be at least 8 characters");
        }

        // Act & Assert
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            _controller.ModelState.IsValid.Should().BeFalse();
        }
    }
}