namespace api.Entities;

/// <summary>A phrase extracted from journal text that maps to a mental health parameter</summary>
public class MatchedItem
{
    public int    Id             { get; set; }
    public int    JournalEntryId { get; set; }

    /// <summary>Parameter code: anx, dep, str, slp, soc, cdt, safe, eng</summary>
    public string Parameter { get; set; } = string.Empty;

    /// <summary>Explanation of why this text matched</summary>
    public string Reason    { get; set; } = string.Empty;

    // Navigation property
    public JournalEntry? JournalEntry { get; set; }
    public ICollection<MatchedItemDetail> Details { get; set; } = new List<MatchedItemDetail>();
}
