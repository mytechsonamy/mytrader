using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Services.Authentication;
using System.Security.Claims;
using ServicesUpdateProfileRequest = MyTrader.Services.Authentication.UpdateProfileRequest;

namespace MyTrader.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Tags("Authentication")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthenticationService authService,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Kayıt sırasında bir hata oluştu."
            });
        }
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<RegisterResponse>> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        try
        {
            var result = await _authService.VerifyEmailAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification error for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Doğrulama sırasında bir hata oluştu."
            });
        }
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<RegisterResponse>> ResendVerification([FromBody] VerificationCodeRequest request)
    {
        try
        {
            var result = await _authService.ResendVerificationAsync(request.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification error for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Kod gönderilirken bir hata oluştu."
            });
        }
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
            _logger.LogWarning("Login failed for email: {Email}. Reason: {Message}", request.Email, ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for email: {Email}", request.Email);
            return StatusCode(500, new { message = "Giriş sırasında bir hata oluştu." });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
            {
                await _authService.LogoutAsync(userId);
            }
            return Ok(new { message = "Başarıyla çıkış yapıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error");
            return StatusCode(500, new { message = "Çıkış sırasında bir hata oluştu." });
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<TokenResponse>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var userAgent = Request.Headers["User-Agent"].ToString();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var result = await _authService.RefreshTokenAsync(request, userAgent, ipAddress);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Token refresh failed: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh error");
            return StatusCode(500, new { message = "Token yenileme sırasında bir hata oluştu." });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
            }

            var user = await _authService.GetUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Kullanıcı bulunamadı." });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get profile error");
            return StatusCode(500, new { message = "Profil bilgileri alınırken bir hata oluştu." });
        }
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<RegisterResponse>> UpdateProfile([FromBody] ServicesUpdateProfileRequest request)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
            }

            var result = await _authService.UpdateUserAsync(userId, request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update profile error");
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Profil güncellenirken bir hata oluştu."
            });
        }
    }

    [HttpPost("request-password-reset")]
    public async Task<ActionResult<RegisterResponse>> RequestPasswordReset([FromBody] PasswordResetRequest request)
    {
        try
        {
            var result = await _authService.RequestPasswordResetAsync(request.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request password reset error for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Şifre sıfırlama isteği gönderilirken bir hata oluştu."
            });
        }
    }

    [HttpPost("verify-password-reset")]
    public async Task<ActionResult<RegisterResponse>> VerifyPasswordReset([FromBody] PasswordResetVerifyRequest request)
    {
        try
        {
            var result = await _authService.VerifyPasswordResetAsync(request.Email, request.VerificationCode);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify password reset error for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Şifre sıfırlama doğrulaması sırasında bir hata oluştu."
            });
        }
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<RegisterResponse>> ResetPassword([FromBody] PasswordResetFinalizeRequest request)
    {
        try
        {
            var result = await _authService.ResetPasswordAsync(request.Email, request.NewPassword);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password error for email: {Email}", request.Email);
            return StatusCode(500, new RegisterResponse
            {
                Success = false,
                Message = "Şifre sıfırlama sırasında bir hata oluştu."
            });
        }
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<ActionResult<SessionListResponse>> GetSessions()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var jwtIdClaim = User.FindFirst("jti")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
            }

            var result = await _authService.GetUserSessionsAsync(userId, jwtIdClaim);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get sessions error");
            return StatusCode(500, new { message = "Oturum bilgileri alınırken bir hata oluştu." });
        }
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<ActionResult> LogoutAll()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
            }

            await _authService.LogoutAllAsync(userId);
            return Ok(new { message = "Tüm oturumlardan çıkış yapıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout all error");
            return StatusCode(500, new { message = "Oturumlardan çıkış sırasında bir hata oluştu." });
        }
    }

    [HttpPost("logout-session/{sessionId}")]
    [Authorize]
    public async Task<ActionResult> LogoutSession(Guid sessionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Geçersiz kullanıcı bilgisi." });
            }

            await _authService.LogoutSessionAsync(userId, sessionId);
            return Ok(new { message = "Oturum sonlandırıldı." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout session error for session: {SessionId}", sessionId);
            return StatusCode(500, new { message = "Oturum sonlandırılırken bir hata oluştu." });
        }
    }
}

