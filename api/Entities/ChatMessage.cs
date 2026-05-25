namespace api.Entities;

public class ChatMessage : AuditableEntity
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    public Chat? Chat { get; set; }
}
