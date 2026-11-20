using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Services;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure;

namespace TaskFlow.API.Services;

public class AuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _email;
    private readonly ILogger<AuthService> _logger;
    private readonly GoogleTokenValidator _googleTokens;

    public AuthService(ApplicationDbContext context, IConfiguration configuration,
        IEmailService email, ILogger<AuthService> logger, GoogleTokenValidator googleTokens)
    {
        _context = context;
        _configuration = configuration;
        _email = email;
        _logger = logger;
        _googleTokens = googleTokens;
    }

    public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
    {
        // Check if email already exists
        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            return null;
        }

        // Hash password
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        // Create user
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = dto.Email,
            PasswordHash = hashedPassword,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Name = $"{dto.FirstName} {dto.LastName}",
            Phone = dto.Phone,
            Role = "Customer",
            EmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await TrySendVerificationEmailAsync(user);

        // Generate tokens (JWT + refresh)
        var (token, expiration, refreshToken, refreshExpiry) = await IssueTokensAsync(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiry,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            return null;
        }

        // Verify password
        if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return null;
        }

        // Check if user is active
        if (!user.IsActive)
        {
            return null;
        }

        // Generate tokens (JWT + refresh)
        var (token, expiration, refreshToken, refreshExpiry) = await IssueTokensAsync(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiry,
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponseDto?> LoginWithGoogleAsync(string idToken)
    {
        var principal = await _googleTokens.ValidateAsync(idToken);
        if (principal is null)
            return null;

        var googleId = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        if (string.IsNullOrWhiteSpace(googleId) || string.IsNullOrWhiteSpace(email))
            return null;

        var emailVerified = bool.TryParse(principal.FindFirst("email_verified")?.Value, out var verified) && verified;
        var firstName = principal.FindFirst("given_name")?.Value ?? string.Empty;
        var lastName = principal.FindFirst("family_name")?.Value ?? string.Empty;
        var name = principal.FindFirst("name")?.Value;
        var picture = principal.FindFirst("picture")?.Value;

        var nameParts = (name ?? $"{firstName} {lastName}").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var resolvedFirstName = firstName.Length > 0 ? firstName : nameParts.FirstOrDefault() ?? "Customer";
        var resolvedLastName = lastName.Length > 0 ? lastName : nameParts.Skip(1).FirstOrDefault() ?? string.Empty;

        var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        if (user is null)
            user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = null,
                GoogleId = googleId,
                FirstName = resolvedFirstName,
                LastName = resolvedLastName,
                Name = name ?? $"{resolvedFirstName} {resolvedLastName}".Trim(),
                AvatarUrl = picture,
                PictureUrl = picture,
                Role = "Customer",
                EmailVerified = emailVerified,
                IsEmailVerified = emailVerified,
                EmailVerifiedAt = emailVerified ? DateTime.UtcNow : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);
        }
        else
        {
            if (string.IsNullOrEmpty(user.GoogleId))
                user.GoogleId = googleId;
            if (string.IsNullOrWhiteSpace(user.FirstName) && !string.IsNullOrWhiteSpace(resolvedFirstName))
                user.FirstName = resolvedFirstName;
            if (string.IsNullOrWhiteSpace(user.LastName) && !string.IsNullOrWhiteSpace(resolvedLastName))
                user.LastName = resolvedLastName;
            if (string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(name))
                user.Name = name;
            user.AvatarUrl ??= picture;
            user.PictureUrl ??= picture;
            if (emailVerified && !user.EmailVerified)
            {
                user.EmailVerified = true;
                user.IsEmailVerified = true;
                user.EmailVerifiedAt ??= DateTime.UtcNow;
            }
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        var (token, expiration, refreshToken, refreshExpiry) = await IssueTokensAsync(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiry,
            User = MapToUserDto(user)
        };
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new JWT + rotated refresh token.
    /// The old refresh token is invalidated (rotation).
    /// </summary>
    public async Task<AuthResponseDto?> RefreshAsync(string rawRefreshToken)
    {
        var hash = HashToken(rawRefreshToken);
        var user = await _context.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == hash && u.RefreshTokenExpiry > DateTime.UtcNow && u.IsActive);
        if (user is null) return null;

        var (token, expiration, refreshToken, refreshExpiry) = await IssueTokensAsync(user);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiry,
            User = MapToUserDto(user)
        };
    }

    /// <summary>Revokes the user's refresh token (logout).</summary>
    public async Task RevokeRefreshTokenAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user is null) return;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _context.SaveChangesAsync();
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? MapToUserDto(user) : null;
    }

    public async Task<UserDto?> UpdateUserAsync(Guid userId, UpdateUserProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        user.FirstName = dto.FirstName;
        user.LastName = dto.LastName;
        user.Phone = dto.Phone;
        user.AvatarUrl = dto.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToUserDto(user);
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return false;

        // Verify current password
        if (user.PasswordHash is null || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            return false;
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Always succeeds silently for unknown emails to avoid user enumeration.
    /// </summary>
    public async Task RequestPasswordResetAsync(string email)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user?.PasswordHash is null) return;

        var token = await CreateAuthTokenAsync(user.Id, AuthTokenPurpose.ResetPassword, TimeSpan.FromHours(1));
        var url = $"{PublicUrl()}/reset-password?token={Uri.EscapeDataString(token)}";

        try
        {
            await _email.SendPasswordResetEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", user.Email);
        }
    }

    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        var authToken = await FindValidTokenAsync(token, AuthTokenPurpose.ResetPassword);
        if (authToken is null) return false;

        var user = await _context.Users.FindAsync(authToken.UserId);
        if (user is null) return false;

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        authToken.UsedAt = DateTime.UtcNow;

        // Invalidate any other outstanding tokens for this user
        var others = await _context.AuthTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync();
        foreach (var other in others)
            other.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> VerifyEmailAsync(string token)
    {
        var authToken = await FindValidTokenAsync(token, AuthTokenPurpose.VerifyEmail);
        if (authToken is null) return false;

        var user = await _context.Users.FindAsync(authToken.UserId);
        if (user is null) return false;

        user.EmailVerified = true;
        user.IsEmailVerified = true;
        user.EmailVerifiedAt = DateTime.UtcNow;
        authToken.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task TrySendVerificationEmailAsync(User user)
    {
        try
        {
            var token = await CreateAuthTokenAsync(user.Id, AuthTokenPurpose.VerifyEmail, TimeSpan.FromDays(2));
            var url = $"{PublicUrl()}/verify-email?token={Uri.EscapeDataString(token)}";
            await _email.SendEmailVerificationEmailAsync(user.Email, $"{user.FirstName} {user.LastName}", url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
        }
    }

    private async Task<AuthToken?> FindValidTokenAsync(string rawToken, AuthTokenPurpose purpose)
    {
        var hash = HashToken(rawToken);
        return await _context.AuthTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash
                && t.Purpose == purpose
                && t.UsedAt == null
                && t.ExpiresAt > DateTime.UtcNow);
    }

    private async Task<string> CreateAuthTokenAsync(Guid userId, AuthTokenPurpose purpose, TimeSpan lifetime)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        _context.AuthTokens.Add(new AuthToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = HashToken(raw),
            Purpose = purpose,
            ExpiresAt = DateTime.UtcNow.Add(lifetime)
        });
        await _context.SaveChangesAsync();
        return raw;
    }

    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(14);

    private async Task<(string Token, DateTime Expires, string RefreshToken, DateTime RefreshExpires)>
        IssueTokensAsync(User user)
    {
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var refreshExpires = DateTime.UtcNow.Add(RefreshTokenLifetime);

        // Only the hash is stored; the raw token is returned once to the client
        user.RefreshToken = HashToken(refreshToken);
        user.RefreshTokenExpiry = refreshExpires;
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return (token, DateTime.UtcNow.AddHours(24), refreshToken, refreshExpires);
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private string PublicUrl() =>
        _configuration["App:PublicUrl"] ?? "http://localhost:5173";

    private string GenerateJwtToken(User user)
    {
        var key = _configuration["Jwt:Key"];
        if (string.IsNullOrEmpty(key))
        {
            key = "YourSuperSecretKeyThatIsAtLeast32CharactersLongForDevelopment123!";
        }
        var issuer = _configuration["Jwt:Issuer"] ?? "TaskFlow";
        var audience = _configuration["Jwt:Audience"] ?? "TaskFlowUsers";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.GivenName, user.FirstName),
            new Claim(ClaimTypes.Surname, user.LastName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var signingKey = new SymmetricSecurityKey(keyBytes);
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            CreatedAt = user.CreatedAt
        };
    }
}

public class UpdateUserProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
}
