namespace api.Entities;

public class JournalTag
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public string Tag { get; set; } = string.Empty;

    public JournalEntry? JournalEntry { get; set; }
}
