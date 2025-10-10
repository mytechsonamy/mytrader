using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using MyTrader.Api;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Tests.Utilities;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace MyTrader.Tests.Integration;

public class AuthenticationIntegrationTests : IntegrationTestBase
{
    public AuthenticationIntegrationTests() : base()
    {
    }

    [Fact]
    public async Task RegisterUser_ValidData_ShouldReturnSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "integration.test@example.com",
            Password = "IntegrationTest123!",
            FirstName = "Integration",
            LastName = "Test",
            Phone = "1234567890"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(responseContent);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUser_DuplicateEmail_ShouldReturnError()
    {
        // Arrange - First registration
        var request1 = new RegisterRequest
        {
            Email = "duplicate.test@example.com",
            Password = "IntegrationTest123!",
            FirstName = "First",
            LastName = "User",
            Phone = "1234567890"
        };

        var json1 = JsonSerializer.Serialize(request1);
        var content1 = new StringContent(json1, Encoding.UTF8, "application/json");

        // First registration
        await Client.PostAsync("/api/auth/register", content1);

        // Second registration with same email
        var request2 = new RegisterRequest
        {
            Email = "duplicate.test@example.com", // Same email
            Password = "IntegrationTest123!",
            FirstName = "Second",
            LastName = "User",
            Phone = "0987654321"
        };

        var json2 = JsonSerializer.Serialize(request2);
        var content2 = new StringContent(json2, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/register", content2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<RegisterResponse>(responseContent);

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("email"); // Should mention email conflict
    }

    [Fact]
    public async Task LoginUser_ValidCredentials_ShouldReturnTokens()
    {
        // Arrange - First register a user
        var registerRequest = new RegisterRequest
        {
            Email = "login.test@example.com",
            Password = "LoginTest123!",
            FirstName = "Login",
            LastName = "Test",
            Phone = "1234567890"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        await Client.PostAsync("/api/auth/register", registerContent);

        // Login request
        var loginRequest = new LoginRequest
        {
            Email = "login.test@example.com",
            Password = "LoginTest123!"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/login", loginContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserSessionResponse>(responseContent);

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("login.test@example.com");
    }

    [Fact]
    public async Task LoginUser_InvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "WrongPassword123!"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/login", loginContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithValidToken_ShouldReturnUserProfile()
    {
        // Arrange - Register and login to get token
        var registerRequest = new RegisterRequest
        {
            Email = "profile.test@example.com",
            Password = "ProfileTest123!",
            FirstName = "Profile",
            LastName = "Test",
            Phone = "1234567890"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        await Client.PostAsync("/api/auth/register", registerContent);

        var loginRequest = new LoginRequest
        {
            Email = "profile.test@example.com",
            Password = "ProfileTest123!"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<UserSessionResponse>(loginResponseContent);

        // Set authorization header
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UserResponse>(responseContent);

        result.Should().NotBeNull();
        result!.Email.Should().Be("profile.test@example.com");
        result.FirstName.Should().Be("Profile");
        result.LastName.Should().Be("Test");
    }

    [Fact]
    public async Task GetProfile_WithoutToken_ShouldReturnUnauthorized()
    {
        // Arrange - No authorization header

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange - Register, login to get tokens
        var registerRequest = new RegisterRequest
        {
            Email = "refresh.test@example.com",
            Password = "RefreshTest123!",
            FirstName = "Refresh",
            LastName = "Test",
            Phone = "1234567890"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        await Client.PostAsync("/api/auth/register", registerContent);

        var loginRequest = new LoginRequest
        {
            Email = "refresh.test@example.com",
            Password = "RefreshTest123!"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<UserSessionResponse>(loginResponseContent);

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = loginResult!.RefreshToken
        };

        var refreshJson = JsonSerializer.Serialize(refreshRequest);
        var refreshContent = new StringContent(refreshJson, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/refresh", refreshContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<TokenResponse>(responseContent);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Logout_WithValidToken_ShouldReturnSuccess()
    {
        // Arrange - Register and login to get token
        var registerRequest = new RegisterRequest
        {
            Email = "logout.test@example.com",
            Password = "LogoutTest123!",
            FirstName = "Logout",
            LastName = "Test",
            Phone = "1234567890"
        };

        var registerJson = JsonSerializer.Serialize(registerRequest);
        var registerContent = new StringContent(registerJson, Encoding.UTF8, "application/json");
        await Client.PostAsync("/api/auth/register", registerContent);

        var loginRequest = new LoginRequest
        {
            Email = "logout.test@example.com",
            Password = "LogoutTest123!"
        };

        var loginJson = JsonSerializer.Serialize(loginRequest);
        var loginContent = new StringContent(loginJson, Encoding.UTF8, "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<UserSessionResponse>(loginResponseContent);

        // Set authorization header
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult!.AccessToken);

        // Act
        var response = await Client.PostAsync("/api/auth/logout", new StringContent("", Encoding.UTF8, "application/json"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("", "ValidPassword123!", "First", "Last", "1234567890")] // Invalid email
    [InlineData("valid@email.com", "weak", "First", "Last", "1234567890")] // Weak password
    [InlineData("valid@email.com", "ValidPassword123!", "", "Last", "1234567890")] // Empty first name
    [InlineData("valid@email.com", "ValidPassword123!", "First", "", "1234567890")] // Empty last name
    [InlineData("valid@email.com", "ValidPassword123!", "First", "Last", "")] // Empty phone
    public async Task RegisterUser_InvalidData_ShouldReturnBadRequest(
        string email, string password, string firstName, string lastName, string phone)
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = email,
            Password = password,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/register", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RegisterResponse>(responseContent);
            result!.Success.Should().BeFalse();
        }
    }

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };
}