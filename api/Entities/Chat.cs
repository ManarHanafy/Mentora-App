namespace api.Entities;

public class Chat : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsEnded { get; set; }
    public DateTime LastActivityAt { get; set; } = DateTime.UtcNow;
    public string? Summary { get; set; }
    public string RiskLevel { get; set; } = "normal";
    public int? TodayMood { get; set; }
    public string? UserMemory { get; set; }

    public User? User { get; set; }
    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
    public ICollection<ChatScoreSnapshot> ScoreSnapshots { get; set; } = new List<ChatScoreSnapshot>();
    public ICollection<ChatScoreTag> Tags { get; set; } = new List<ChatScoreTag>();
}
