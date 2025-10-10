using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using AutoFixture;
using MyTrader.Api.Controllers;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Core.Interfaces;
using MyTrader.Core.Models;
using Xunit;

namespace MyTrader.Tests.Unit.Controllers;

/// <summary>
/// Unit tests for AuthController - Testing authentication and authorization logic
/// Follows AAA pattern: Arrange, Act, Assert
/// </summary>
public class AuthControllerTests
{
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IAuthenticationService> _authServiceMock;
    private readonly AuthController _controller;
    private readonly IFixture _fixture;

    public AuthControllerTests()
    {
        _loggerMock = new Mock<ILogger<AuthController>>();
        _authServiceMock = new Mock<IAuthenticationService>();
        _fixture = new Fixture();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOkWithToken()
    {
        // Arrange
        var loginRequest = _fixture.Build<LoginRequest>()
            .With(x => x.Email, "test@example.com")
            .With(x => x.Password, "ValidPassword123!")
            .Create();

        var expectedResponse = _fixture.Create<LoginResponse>();
        _authServiceMock.Setup(x => x.LoginAsync(loginRequest))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
        
        _authServiceMock.Verify(x => x.LoginAsync(loginRequest), Times.Once);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var loginRequest = _fixture.Build<LoginRequest>()
            .With(x => x.Email, "invalid@example.com")
            .With(x => x.Password, "wrongpassword")
            .Create();

        _authServiceMock.Setup(x => x.LoginAsync(loginRequest))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Theory]
    [InlineData("", "ValidPassword123!")]
    [InlineData("invalid-email", "ValidPassword123!")]
    [InlineData("test@example.com", "")]
    [InlineData("test@example.com", "weak")]
    public async Task Login_WithInvalidInput_ReturnsBadRequest(string email, string password)
    {
        // Arrange
        var loginRequest = new LoginRequest { Email = email, Password = password };
        
        // Simulate model validation failure
        _controller.ModelState.AddModelError("Email", "Invalid email format");

        // Act
        var result = await _controller.Login(loginRequest);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsCreated()
    {
        // Arrange
        var registerRequest = _fixture.Build<RegisterRequest>()
            .With(x => x.Email, "newuser@example.com")
            .With(x => x.Password, "StrongPassword123!")
            .Create();

        var expectedUser = _fixture.Create<User>();
        _authServiceMock.Setup(x => x.RegisterAsync(registerRequest))
            .ReturnsAsync(expectedUser);

        // Act
        var result = await _controller.Register(registerRequest);

        // Assert
        result.Should().BeOfType<CreatedAtActionResult>();
        var createdResult = result as CreatedAtActionResult;
        createdResult!.Value.Should().BeEquivalentTo(expectedUser);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsConflict()
    {
        // Arrange
        var registerRequest = _fixture.Create<RegisterRequest>();
        _authServiceMock.Setup(x => x.RegisterAsync(registerRequest))
            .ThrowsAsync(new InvalidOperationException("User already exists"));

        // Act
        var result = await _controller.Register(registerRequest);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsOkWithNewToken()
    {
        // Arrange
        var refreshTokenRequest = _fixture.Create<RefreshTokenRequest>();
        var expectedResponse = _fixture.Create<LoginResponse>();
        
        _authServiceMock.Setup(x => x.RefreshTokenAsync(refreshTokenRequest))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.RefreshToken(refreshTokenRequest);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(expectedResponse);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var refreshTokenRequest = _fixture.Create<RefreshTokenRequest>();
        _authServiceMock.Setup(x => x.RefreshTokenAsync(refreshTokenRequest))
            .ThrowsAsync(new UnauthorizedAccessException("Invalid refresh token"));

        // Act
        var result = await _controller.RefreshToken(refreshTokenRequest);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Logout_WithValidRequest_ReturnsOk()
    {
        // Arrange
        var logoutRequest = _fixture.Create<LogoutRequest>();
        _authServiceMock.Setup(x => x.LogoutAsync(logoutRequest))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout(logoutRequest);

        // Assert
        result.Should().BeOfType<OkResult>();
        _authServiceMock.Verify(x => x.LogoutAsync(logoutRequest), Times.Once);
    }
}