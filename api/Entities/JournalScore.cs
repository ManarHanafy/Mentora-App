namespace api.Entities;

/// <summary>Persisted score snapshot for a single journal entry.</summary>
public class JournalScore
{
    public int Id { get; set; }
    public int JournalEntryId { get; set; }
    public int Anx { get; set; }
    public int Dep { get; set; }
    public int Str { get; set; }
    public int Slp { get; set; }
    public int Soc { get; set; }
    public int Cdt { get; set; }
    public int Safe { get; set; }
    public int Eng { get; set; }

    public JournalEntry? JournalEntry { get; set; }
}
