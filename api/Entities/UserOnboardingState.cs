namespace api.Entities;

public class UserOnboardingState : AuditableEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? RawResponsesJson { get; set; }

    public User? User { get; set; }
    public UserOnboardingResult? Result { get; set; }
    public ICollection<UserOnboardingResponse> Responses { get; set; } = new List<UserOnboardingResponse>();
}
