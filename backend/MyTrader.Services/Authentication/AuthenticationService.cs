using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace MyTrader.Services.Authentication;

public class AuthenticationService : IAuthenticationService
{
    private readonly TradingDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthenticationService> _logger;
    private readonly IEmailService _emailService;

    public AuthenticationService(
        TradingDbContext context, 
        IConfiguration configuration, 
        ILogger<AuthenticationService> logger,
        IEmailService emailService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _emailService = emailService;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Bu email adresi zaten kullanılıyor."
                };
            }

            // Validate password
            var passwordValidation = ValidatePasswordStrength(request.Password);
            if (!passwordValidation.IsValid)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = passwordValidation.Message
                };
            }

            // Hash password
            var passwordHash = HashPassword(request.Password);

            // Generate verification code
            var verificationCode = GenerateVerificationCode();

            // Store verification code
            var verification = new EmailVerification
            {
                Email = request.Email,
                VerificationCode = verificationCode,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            await _context.EmailVerifications.AddAsync(verification);

            // Store temporary registration
            var tempRegistration = new TempRegistration
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Phone = request.Phone
            };

            await _context.TempRegistrations.AddAsync(tempRegistration);
            await _context.SaveChangesAsync();

            // Send verification email
            await _emailService.SendVerificationEmailAsync(request.Email, verificationCode);

            return new RegisterResponse
            {
                Success = true,
                Message = "Email adresinize doğrulama kodu gönderildi. Hesabınızı aktifleştirmek için kodu giriniz."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error for email: {Email}", request.Email);
            return new RegisterResponse
            {
                Success = false,
                Message = "Kayıt sırasında bir hata oluştu."
            };
        }
    }

    public async Task<RegisterResponse> VerifyEmailAsync(VerifyEmailRequest request)
    {
        try
        {
            // Check verification code
            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == request.Email && v.ExpiresAt > DateTime.UtcNow);

            if (verification == null || verification.VerificationCode != request.VerificationCode)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Geçersiz veya süresi dolmuş doğrulama kodu."
                };
            }

            // Get temp registration data
            var tempData = await _context.TempRegistrations
                .FirstOrDefaultAsync(t => t.Email == request.Email);

            if (tempData == null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Kayıt verileri bulunamadı. Lütfen tekrar kayıt olun."
                };
            }

            // Create actual user account
            var user = new User
            {
                Email = request.Email,
                PasswordHash = tempData.PasswordHash,
                FirstName = tempData.FirstName,
                LastName = tempData.LastName,
                Phone = tempData.Phone,
                IsActive = true,
                IsEmailVerified = true
            };

            await _context.Users.AddAsync(user);

            // Clean up temporary data
            _context.EmailVerifications.Remove(verification);
            _context.TempRegistrations.Remove(tempData);
            
            await _context.SaveChangesAsync();

            return new RegisterResponse
            {
                Success = true,
                Message = "Hesabınız başarıyla oluşturuldu! Şimdi giriş yapabilirsiniz."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification error for email: {Email}", request.Email);
            return new RegisterResponse
            {
                Success = false,
                Message = "Doğrulama sırasında bir hata oluştu."
            };
        }
    }

    public async Task<RegisterResponse> ResendVerificationAsync(string email)
    {
        try
        {
            var tempRegistration = await _context.TempRegistrations
                .FirstOrDefaultAsync(t => t.Email == email);

            if (tempRegistration == null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Bu email için bekleyen bir kayıt bulunamadı."
                };
            }

            // Check throttling
            var existingVerification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == email);

            if (existingVerification != null && 
                (DateTime.UtcNow - existingVerification.CreatedAt).TotalSeconds < 60)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Çok sık istek yapıldı. Lütfen 1 dakika sonra tekrar deneyin."
                };
            }

            var verificationCode = GenerateVerificationCode();
            
            if (existingVerification != null)
            {
                existingVerification.VerificationCode = verificationCode;
                existingVerification.ExpiresAt = DateTime.UtcNow.AddMinutes(10);
                existingVerification.CreatedAt = DateTime.UtcNow;
                _context.EmailVerifications.Update(existingVerification);
            }
            else
            {
                var newVerification = new EmailVerification
                {
                    Email = email,
                    VerificationCode = verificationCode,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10)
                };
                await _context.EmailVerifications.AddAsync(newVerification);
            }

            await _context.SaveChangesAsync();
            await _emailService.SendVerificationEmailAsync(email, verificationCode);

            return new RegisterResponse
            {
                Success = true,
                Message = "Yeni doğrulama kodu email adresinize gönderildi."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification error for email: {Email}", email);
            return new RegisterResponse
            {
                Success = false,
                Message = "Doğrulama kodu gönderilirken bir hata oluştu."
            };
        }
    }

    public async Task<UserSessionResponse> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Geçersiz email veya şifre");
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(24);

            // Update last login and create session
            user.LastLogin = DateTime.UtcNow;
            
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = token,
                ExpiresAt = expiresAt
            };

            await _context.UserSessions.AddAsync(session);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new UserSessionResponse
            {
                SessionToken = token,
                User = new UserResponse
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Phone = user.Phone,
                    TelegramId = user.TelegramId,
                    IsActive = user.IsActive,
                    IsEmailVerified = user.IsEmailVerified,
                    LastLogin = user.LastLogin,
                    CreatedAt = user.CreatedAt
                },
                ExpiresAt = expiresAt
            };
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for email: {Email}", request.Email);
            throw new InvalidOperationException("Giriş sırasında bir hata oluştu");
        }
    }

    public async Task LogoutAsync(Guid userId)
    {
        try
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId)
                .ToListAsync();

            _context.UserSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Logout error for user: {UserId}", userId);
            throw new InvalidOperationException("Çıkış sırasında bir hata oluştu");
        }
    }

    public async Task<UserResponse?> GetUserAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);

            if (user == null) return null;

            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                TelegramId = user.TelegramId,
                IsActive = user.IsActive,
                IsEmailVerified = user.IsEmailVerified,
                LastLogin = user.LastLogin,
                CreatedAt = user.CreatedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get user error for user: {UserId}", userId);
            throw new InvalidOperationException("Kullanıcı bilgileri alınırken bir hata oluştu");
        }
    }

    public async Task<RegisterResponse> UpdateUserAsync(Guid userId, UpdateProfileRequest request)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı"
                };
            }

            bool hasChanges = false;

            if (!string.IsNullOrEmpty(request.FirstName))
            {
                user.FirstName = request.FirstName;
                hasChanges = true;
            }

            if (!string.IsNullOrEmpty(request.LastName))
            {
                user.LastName = request.LastName;
                hasChanges = true;
            }

            if (request.Phone != null)
            {
                user.Phone = request.Phone;
                hasChanges = true;
            }

            if (request.TelegramId != null)
            {
                user.TelegramId = request.TelegramId;
                hasChanges = true;
            }

            if (!hasChanges)
            {
                return new RegisterResponse
                {
                    Success = true,
                    Message = "Güncellenecek alan yok"
                };
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new RegisterResponse
            {
                Success = true,
                Message = "Profil güncellendi"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update user error for user: {UserId}", userId);
            return new RegisterResponse
            {
                Success = false,
                Message = "Profil güncellenemedi"
            };
        }
    }

    public async Task<RegisterResponse> RequestPasswordResetAsync(string email)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

            if (user == null || !user.IsActive)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = user == null ? "Kullanıcı bulunamadı" : "Hesap aktif değil. Lütfen email doğrulamasını tamamlayın."
                };
            }

            // Check throttling
            var existingVerification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == email);

            if (existingVerification != null &&
                (DateTime.UtcNow - existingVerification.CreatedAt).TotalSeconds < 60)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Çok sık istek yapıldı. Lütfen 1 dakika sonra tekrar deneyin."
                };
            }

            var code = GenerateVerificationCode();
            var expireTime = DateTime.UtcNow.AddMinutes(10);

            if (existingVerification != null)
            {
                existingVerification.VerificationCode = code;
                existingVerification.ExpiresAt = expireTime;
                existingVerification.CreatedAt = DateTime.UtcNow;
                _context.EmailVerifications.Update(existingVerification);
            }
            else
            {
                var newVerification = new EmailVerification
                {
                    Email = email,
                    VerificationCode = code,
                    ExpiresAt = expireTime
                };
                await _context.EmailVerifications.AddAsync(newVerification);
            }

            await _context.SaveChangesAsync();
            await _emailService.SendPasswordResetEmailAsync(email, code);

            return new RegisterResponse
            {
                Success = true,
                Message = "Doğrulama kodu gönderildi"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request password reset error for email: {Email}", email);
            return new RegisterResponse
            {
                Success = false,
                Message = "İşlem başarısız"
            };
        }
    }

    public async Task<RegisterResponse> VerifyPasswordResetAsync(string email, string code)
    {
        try
        {
            // In test mode, accept default code
            if (_configuration.GetValue<bool>("AuthTestMode") && code == "111111")
            {
                _logger.LogInformation("Test mode: Accepting default verification code for {Email}", email);
                return new RegisterResponse { Success = true, Message = "Doğrulandı" };
            }

            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == email && v.ExpiresAt > DateTime.UtcNow);

            if (verification == null || verification.VerificationCode != code)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Geçersiz doğrulama kodu"
                };
            }

            return new RegisterResponse
            {
                Success = true,
                Message = "Doğrulandı"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify password reset error for email: {Email}", email);
            return new RegisterResponse
            {
                Success = false,
                Message = "İşlem başarısız"
            };
        }
    }

    public async Task<RegisterResponse> ResetPasswordAsync(string email, string newPassword)
    {
        try
        {
            var passwordValidation = ValidatePasswordStrength(newPassword);
            if (!passwordValidation.IsValid)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = passwordValidation.Message
                };
            }

            var hashedPassword = HashPassword(newPassword);
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (user == null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Kullanıcı bulunamadı"
                };
            }

            user.PasswordHash = hashedPassword;
            _context.Users.Update(user);

            // Remove verification
            var verification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == email);
            if (verification != null)
            {
                _context.EmailVerifications.Remove(verification);
            }

            await _context.SaveChangesAsync();

            return new RegisterResponse
            {
                Success = true,
                Message = "Şifre güncellendi"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset password error for email: {Email}", email);
            return new RegisterResponse
            {
                Success = false,
                Message = "Şifre güncellenemedi"
            };
        }
    }

    private string HashPassword(string password)
    {
        using var rng = RandomNumberGenerator.Create();
        var saltBytes = new byte[16];
        rng.GetBytes(saltBytes);
        var salt = Convert.ToHexString(saltBytes);

        using var sha256 = SHA256.Create();
        var passwordBytes = Encoding.UTF8.GetBytes(password + salt);
        var hashBytes = sha256.ComputeHash(passwordBytes);
        var hash = Convert.ToHexString(hashBytes);

        return $"{salt}:{hash}";
    }

    private bool VerifyPassword(string password, string hashedPassword)
    {
        try
        {
            var parts = hashedPassword.Split(':');
            if (parts.Length != 2) return false;

            var salt = parts[0];
            var hash = parts[1];

            using var sha256 = SHA256.Create();
            var passwordBytes = Encoding.UTF8.GetBytes(password + salt);
            var computedHashBytes = sha256.ComputeHash(passwordBytes);
            var computedHash = Convert.ToHexString(computedHashBytes);

            return hash.Equals(computedHash, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private (bool IsValid, string Message) ValidatePasswordStrength(string password)
    {
        if (password.Length < 8)
            return (false, "Şifre en az 8 karakter olmalıdır.");

        var hasUpper = password.Any(char.IsUpper);
        var hasLower = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);

        if (!(hasUpper && hasLower && hasDigit))
            return (false, "Şifre büyük/küçük harf ve rakam içermelidir.");

        return (true, string.Empty);
    }

    private string GenerateVerificationCode()
    {
        if (_configuration.GetValue<bool>("AuthTestMode"))
        {
            return "111111";
        }

        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[4];
        rng.GetBytes(bytes);
        var number = BitConverter.ToUInt32(bytes, 0) % 1000000;
        return number.ToString("D6");
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSecret = _configuration["Jwt:Secret"] ?? "your-secret-key-change-in-production";
        var key = Encoding.ASCII.GetBytes(jwtSecret);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("user_id", user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            }),
            Expires = DateTime.UtcNow.AddHours(24),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string code);
    Task SendPasswordResetEmailAsync(string email, string code);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendVerificationEmailAsync(string email, string code)
    {
        if (_configuration.GetValue<bool>("AuthTestMode"))
        {
            _logger.LogInformation("Test mode: verification code for {Email} is {Code}", email, code);
            return;
        }

        // TODO: Implement actual email sending using SMTP
        _logger.LogInformation("Would send verification email to {Email} with code {Code}", email, code);
        await Task.CompletedTask;
    }

    public async Task SendPasswordResetEmailAsync(string email, string code)
    {
        if (_configuration.GetValue<bool>("AuthTestMode"))
        {
            _logger.LogInformation("Test mode: password reset code for {Email} is {Code}", email, code);
            return;
        }

        // TODO: Implement actual email sending using SMTP
        _logger.LogInformation("Would send password reset email to {Email} with code {Code}", email, code);
        await Task.CompletedTask;
    }
}