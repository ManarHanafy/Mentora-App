namespace api.Entities;

/// <summary>
/// Owned entity that stores a user's refresh token.
/// Stored as a separate table: User_RefreshTokens.
/// </summary>
public class RefreshToken
{
    public string    Token     { get; set; } = string.Empty;
    public DateTime  ExpiresOn { get; set; }
    public DateTime  CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedOn { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresOn;
    public bool IsActive  => RevokedOn is null && !IsExpired;
}
