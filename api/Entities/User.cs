namespace api.Entities;

/// <summary>User domain model</summary>
public class User
{
    public int      Id                { get; set; }
    public string   Username           { get; set; } = string.Empty;
    public string   Email              { get; set; } = string.Empty;
    public string   FirstName          { get; set; } = string.Empty;
    public string   LastName           { get; set; } = string.Empty;
    public string   PasswordHash       { get; set; } = string.Empty;
    public bool     IsActive           { get; set; } = true;
    public bool     EmailVerified      { get; set; }
    public DateTime? EmailVerifiedAt    { get; set; }
    public string?  EmailOtpHash        { get; set; }
    public DateTime? EmailOtpExpiresAt  { get; set; }
    public string   Role               { get; set; } = "User";
    public DateTime CreatedAt          { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin         { get; set; }
    public DateTime? PasswordChangedAt { get; set; }
    public int FailedLoginCount        { get; set; }
    public DateTime? LockoutUntil      { get; set; }

    // Navigation properties
    public ICollection<JournalEntry>    JournalEntries    { get; set; } = new List<JournalEntry>();
    public UserParameterSnapshot?       ParameterSnapshot { get; set; }
    public List<RefreshToken>           RefreshTokens     { get; set; } = new();
    public ICollection<Chat>            Chats             { get; set; } = new List<Chat>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
}
