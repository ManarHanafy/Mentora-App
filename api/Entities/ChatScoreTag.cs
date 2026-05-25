namespace api.Entities;

public class ChatScoreTag : AuditableEntity
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string Tag { get; set; } = string.Empty;

    public Chat? Chat { get; set; }
}
