namespace api.Entities;

public class UserOnboardingResult : AuditableEntity
{
    public int Id { get; set; }
    public int UserOnboardingStateId { get; set; }
    public int UserId { get; set; }
    public DateTime CompletedAt { get; set; }

    public int Anx { get; set; }
    public int Dep { get; set; }
    public int Str { get; set; }
    public int Slp { get; set; }
    public int Soc { get; set; }
    public int Cdt { get; set; }
    public int Safe { get; set; }
    public int Eng { get; set; }

    public UserOnboardingState? State { get; set; }
}
