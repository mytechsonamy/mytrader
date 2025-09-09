using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Services.Authentication;
using System.Security.Claims;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;

    public AuthController(IAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        return Ok(result);
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<RegisterResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var result = await _authService.VerifyEmailAsync(request);
        return Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<RegisterResponse>> ResendVerification([FromBody] VerificationCodeRequest request)
    {
        var result = await _authService.ResendVerificationAsync(request.Email);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<UserSessionResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { code = "AUTH_INVALID", message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { code = "LOGIN_ERROR", message = ex.Message });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _authService.LogoutAsync(userId);
        }
        
        return Ok(new { message = "Başarıyla çıkış yapıldı" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetMe()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Geçersiz token" });
        }

        var user = await _authService.GetUserAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Kullanıcı bulunamadı" });
        }

        return Ok(user);
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<RegisterResponse>> UpdateMe([FromBody] UpdateProfileRequest request)
    {
        var userIdClaim = User.FindFirst("user_id")?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Geçersiz token" });
        }

        var result = await _authService.UpdateUserAsync(userId, request);
        return Ok(result);
    }

    [HttpPost("request-password-reset")]
    public async Task<ActionResult<RegisterResponse>> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        var result = await _authService.RequestPasswordResetAsync(request.Email);
        return Ok(result);
    }

    [HttpPost("verify-password-reset")]
    public async Task<ActionResult<RegisterResponse>> VerifyPasswordReset([FromBody] PasswordResetVerifyRequest request)
    {
        var result = await _authService.VerifyPasswordResetAsync(request.Email, request.VerificationCode);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<RegisterResponse>> ResetPassword([FromBody] PasswordResetFinalizeRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request.Email, request.NewPassword);
        return Ok(result);
    }
}

public class VerificationCodeRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetRequest
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordResetVerifyRequest
{
    public string Email { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}

public class PasswordResetFinalizeRequest
{
    public string Email { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}