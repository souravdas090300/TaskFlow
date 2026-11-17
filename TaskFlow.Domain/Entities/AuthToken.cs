namespace TaskFlow.Domain.Entities;

public enum AuthTokenPurpose
{
    VerifyEmail,
    ResetPassword
}

/// <summary>
/// Single-use token for email verification and password reset.
/// Only the SHA-256 hash is stored; the raw token is emailed to the user.
/// </summary>
public class AuthToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public AuthTokenPurpose Purpose { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
