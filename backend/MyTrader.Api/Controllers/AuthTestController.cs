using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs.Authentication;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/auth-test")]
[Tags("Authentication")]
public class AuthTestController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthTestController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("register")]
    public ActionResult<RegisterResponse> Register([FromBody] RegisterRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Test registration successful - no verification needed in test mode"
        });
    }

    [HttpPost("login")]
    public ActionResult<UserSessionResponse> Login([FromBody] LoginRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new UserSessionResponse
        {
            AccessToken = "test_token_12345",
            RefreshToken = "test_refresh_token_12345",
            AccessTokenExpiresAt = DateTime.UtcNow.AddHours(1),
            RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(7),
            JwtId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid(),
            User = new UserResponse
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = "Test",
                LastName = "User",
                IsEmailVerified = true
            }
        });
    }

    [HttpPost("verify-email")]
    public ActionResult<RegisterResponse> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Test email verification successful"
        });
    }

    [HttpPost("resend-verification")]
    public ActionResult<RegisterResponse> ResendVerification([FromBody] VerificationCodeRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Test verification code sent"
        });
    }

    [HttpPost("request-password-reset")]
    public ActionResult<RegisterResponse> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Test password reset requested"
        });
    }

    [HttpPost("verify-password-reset")]
    public ActionResult<RegisterResponse> VerifyPasswordReset([FromBody] PasswordResetVerifyRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Test password reset verification successful"
        });
    }

    [HttpPost("reset-password")]
    public ActionResult<RegisterResponse> ResetPassword([FromBody] PasswordResetFinalizeRequest request)
    {
        if (!_configuration.GetValue<bool>("AuthTestMode"))
        {
            return BadRequest("Test mode not enabled");
        }

        return Ok(new RegisterResponse
        {
            Success = true,
            Message = "Test password reset successful"
        });
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