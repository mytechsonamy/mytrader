using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using MyTrader.Core.DTOs.Authentication;
using MyTrader.Core.Models;
using MyTrader.Infrastructure.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security;
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
            _logger.LogInformation("Starting registration process for email: {Email}", request.Email);
            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return new RegisterResponse
                {
                    Success = false,
                    Message = "Bu email adresi ile zaten bir hesap bulunmaktadır. Eğer şifrenizi unuttuysanız 'Şifremi Unuttum' seçeneğini kullanabilirsiniz."
                };
            }

            // Check if user exists with phone number
            if (!string.IsNullOrEmpty(request.Phone))
            {
                var existingUserByPhone = await _context.Users.FirstOrDefaultAsync(u => u.Phone == request.Phone);
                if (existingUserByPhone != null)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Bu telefon numarası ile zaten bir hesap bulunmaktadır. Eğer şifrenizi unuttuysanız 'Şifremi Unuttum' seçeneğini kullanabilirsiniz."
                    };
                }
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

            // Clean up any existing records for this email since user doesn't exist
            var existingVerification = await _context.EmailVerifications
                .FirstOrDefaultAsync(v => v.Email == request.Email);
            var existingTempRegistration = await _context.TempRegistrations
                .FirstOrDefaultAsync(t => t.Email == request.Email);

            // Remove existing records if they exist
            if (existingVerification != null)
            {
                _context.EmailVerifications.Remove(existingVerification);
            }

            if (existingTempRegistration != null)
            {
                _context.TempRegistrations.Remove(existingTempRegistration);
            }

            // Save removals first
            if (existingVerification != null || existingTempRegistration != null)
            {
                await _context.SaveChangesAsync();
            }

            // Store new verification code
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
            _logger.LogError(ex, "Registration error for email: {Email}. Error: {ErrorMessage}", request.Email, ex.Message);
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
                _logger.LogWarning(
                    "Email verification failed for {Email}. StoredVerificationCode={StoredCode}, ProvidedVerificationCode={ProvidedCode}",
                    request.Email,
                    verification?.VerificationCode ?? "null",
                    request.VerificationCode ?? "null");

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

            // Generate tokens with new system
            var jwtId = Guid.NewGuid().ToString();
            var accessToken = GenerateJwtToken(user, jwtId);
            var refreshToken = GenerateRefreshToken();
            var refreshTokenHash = HashRefreshToken(refreshToken);
            
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Short-lived access token
            var refreshTokenExpiry = DateTime.UtcNow.AddDays(30); // Long-lived refresh token

            // Update last login and create session
            user.LastLogin = DateTime.UtcNow;
            
            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = accessToken, // Keep this for backward compatibility, but use JwtId for new logic
                JwtId = jwtId,
                RefreshTokenHash = refreshTokenHash,
                TokenFamilyId = Guid.NewGuid(), // New token family
                ExpiresAt = refreshTokenExpiry
            };

            await _context.UserSessions.AddAsync(session);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return new UserSessionResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
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
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Plan = user.Plan ?? "free"
                },
                AccessTokenExpiresAt = accessTokenExpiry,
                RefreshTokenExpiresAt = refreshTokenExpiry,
                JwtId = jwtId,
                SessionId = session.Id
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

    // Enhanced Session Management Methods

    public async Task<TokenResponse> RefreshTokenAsync(RefreshTokenRequest request, string? userAgent = null, string? ipAddress = null)
    {
        try
        {
            // Hash the provided refresh token to compare with stored hash
            var refreshTokenHash = HashRefreshToken(request.RefreshToken);
            
            // Find the session with this refresh token hash
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.RefreshTokenHash == refreshTokenHash && s.RevokedAt == null);

            if (session == null || session.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("Invalid or expired refresh token used from IP: {IpAddress}", ipAddress);
                throw new UnauthorizedAccessException("Invalid or expired refresh token");
            }

            // Check for token reuse - if this token was already used, it's a sign of compromise
            if (session.LastUsedAt.HasValue)
            {
                _logger.LogError("Token reuse detected for user {UserId} from IP {IpAddress}. Revoking token family {TokenFamilyId}", 
                    session.UserId, ipAddress, session.TokenFamilyId);
                
                // Revoke the entire token family
                await RevokeTokenFamilyAsync(session.TokenFamilyId, "token_reuse");
                
                throw new SecurityException("Token reuse detected. All sessions revoked for security.");
            }

            // Mark this token as used
            session.LastUsedAt = DateTime.UtcNow;

            // Create new session with rotated tokens
            var newJwtId = Guid.NewGuid().ToString();
            var newRefreshToken = GenerateRefreshToken();
            var newRefreshTokenHash = HashRefreshToken(newRefreshToken);

            var newSession = new UserSession
            {
                UserId = session.UserId,
                SessionToken = "", // Will be set to access token
                JwtId = newJwtId,
                RefreshTokenHash = newRefreshTokenHash,
                TokenFamilyId = session.TokenFamilyId, // Keep same family
                RotatedFrom = session.Id, // Track rotation chain
                UserAgent = userAgent,
                IpAddress = ipAddress,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30-day refresh token expiry
                CreatedAt = DateTime.UtcNow
            };

            // Revoke the old session
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "token_rotated";

            // Add new session
            _context.UserSessions.Add(newSession);
            
            // Generate new access token
            var accessToken = GenerateJwtToken(session.User, newJwtId);
            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(15); // Short-lived access token
            
            // Update the session token for backward compatibility
            newSession.SessionToken = accessToken;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Token refreshed successfully for user {UserId} from IP {IpAddress}", 
                session.UserId, ipAddress);

            return new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiresAt = accessTokenExpiry,
                RefreshTokenExpiresAt = newSession.ExpiresAt,
                JwtId = newJwtId,
                TokenType = "Bearer"
            };
        }
        catch (SecurityException)
        {
            throw; // Re-throw security exceptions
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh from IP: {IpAddress}", ipAddress);
            throw new UnauthorizedAccessException("Token refresh failed");
        }
    }

    public async Task<SessionListResponse> GetUserSessionsAsync(Guid userId, string? currentJwtId = null)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(s => s.LastUsedAt ?? s.CreatedAt)
            .Select(s => new SessionInfo
            {
                Id = s.Id,
                JwtId = s.JwtId,
                DeviceName = ExtractDeviceName(s.UserAgent),
                UserAgent = s.UserAgent,
                IpAddress = s.IpAddress,
                CreatedAt = s.CreatedAt,
                LastUsedAt = s.LastUsedAt ?? s.CreatedAt,
                ExpiresAt = s.ExpiresAt,
                IsCurrentSession = s.JwtId == currentJwtId
            })
            .ToListAsync();

        return new SessionListResponse { Sessions = sessions };
    }

    public async Task LogoutAllAsync(Guid userId)
    {
        var activeSessions = await _context.UserSessions
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToListAsync();

        foreach (var session in activeSessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "user_logout_all";
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("All sessions revoked for user {UserId} ({SessionCount} sessions)", 
            userId, activeSessions.Count);
    }

    public async Task LogoutSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _context.UserSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId && s.RevokedAt == null);

        if (session != null)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "user_logout_session";
            
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Session {SessionId} revoked for user {UserId}", sessionId, userId);
        }
    }

    public async Task<UserResponse?> ValidateTokenAsync(string jwtId, Guid userId)
    {
        // Check if the JWT ID is associated with an active session
        var session = await _context.UserSessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.JwtId == jwtId && s.UserId == userId && s.RevokedAt == null && s.ExpiresAt > DateTime.UtcNow);

        if (session?.User == null || !session.User.IsActive)
        {
            return null;
        }

        return new UserResponse
        {
            Id = session.User.Id,
            Email = session.User.Email,
            FirstName = session.User.FirstName,
            LastName = session.User.LastName,
            Phone = session.User.Phone,
            TelegramId = session.User.TelegramId,
            IsActive = session.User.IsActive,
            IsEmailVerified = session.User.IsEmailVerified,
            CreatedAt = session.User.CreatedAt,
            UpdatedAt = session.User.UpdatedAt,
            Plan = session.User.Plan ?? "free"
        };
    }

    public async Task RevokeTokenFamilyAsync(Guid tokenFamilyId, string reason = "token_reuse")
    {
        var familySessions = await _context.UserSessions
            .Where(s => s.TokenFamilyId == tokenFamilyId && s.RevokedAt == null)
            .ToListAsync();

        foreach (var session in familySessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = reason;
        }

        await _context.SaveChangesAsync();

        _logger.LogWarning("Token family {TokenFamilyId} revoked. Reason: {Reason}. Sessions affected: {SessionCount}", 
            tokenFamilyId, reason, familySessions.Count);
    }

    // Helper Methods for Session Management

    private string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private string HashRefreshToken(string refreshToken)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToBase64String(hashedBytes);
    }

    private string GenerateJwtToken(User user, string jwtId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Standard subject claim
            new Claim("sub", user.Id.ToString()), // Explicit sub claim for SignalR
            new Claim("user_id", user.Id.ToString()),
            new Claim("email", user.Email),
            new Claim("first_name", user.FirstName),
            new Claim("last_name", user.LastName),
            new Claim("jti", jwtId), // JWT ID for session linking
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15), // Short-lived access token
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string? ExtractDeviceName(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown Device";

        // Simple device name extraction - can be enhanced
        if (userAgent.Contains("iPhone"))
            return "iPhone";
        if (userAgent.Contains("iPad"))
            return "iPad";
        if (userAgent.Contains("Android"))
            return "Android Device";
        if (userAgent.Contains("Windows"))
            return "Windows PC";
        if (userAgent.Contains("Macintosh"))
            return "Mac";
        if (userAgent.Contains("Linux"))
            return "Linux PC";

        return "Web Browser";
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

    private bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying password");
            return false;
        }
    }
}
