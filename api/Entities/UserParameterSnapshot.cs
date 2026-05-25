namespace api.Entities;

/// <summary>
/// Rolling snapshot — one row per user, updated after every journal analysis.
/// Scale 0–20: higher = more severe (except eng where higher = better engagement).
/// </summary>
public class UserParameterSnapshot
{
    public int  Id                   { get; set; }
    public int  UserId               { get; set; }
    public int? LatestJournalEntryId { get; set; }

    /// <summary>
    /// Parameters: anx, dep, str, slp, soc, cdt, safe, eng
    /// Stored as individual properties for database compatibility
    /// </summary>
    public int Anx  { get; set; }
    public int Dep  { get; set; }
    public int Str  { get; set; }
    public int Slp  { get; set; }
    public int Soc  { get; set; }
    public int Cdt  { get; set; }
    public int Safe { get; set; }
    public int Eng  { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User?         User               { get; set; }
    public JournalEntry? LatestJournalEntry { get; set; }

    /// <summary>Convert to dictionary format for API responses</summary>
    public Dictionary<string, int> ToParametersDictionary() => new()
    {
        { "anx", Anx },
        { "dep", Dep },
        { "str", Str },
        { "slp", Slp },
        { "soc", Soc },
        { "cdt", Cdt },
        { "safe", Safe },
        { "eng", Eng }
    };

    /// <summary>Update from dictionary</summary>
    public void UpdateFromDictionary(Dictionary<string, int> parameters)
    {
        if (parameters.TryGetValue("anx", out var anx)) Anx = anx;
        if (parameters.TryGetValue("dep", out var dep)) Dep = dep;
        if (parameters.TryGetValue("str", out var str)) Str = str;
        if (parameters.TryGetValue("slp", out var slp)) Slp = slp;
        if (parameters.TryGetValue("soc", out var soc)) Soc = soc;
        if (parameters.TryGetValue("cdt", out var cdt)) Cdt = cdt;
        if (parameters.TryGetValue("safe", out var safe)) Safe = safe;
        if (parameters.TryGetValue("eng", out var eng)) Eng = eng;
        UpdatedAt = DateTime.UtcNow;
    }
}
