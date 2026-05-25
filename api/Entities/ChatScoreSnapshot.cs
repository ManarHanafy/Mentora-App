namespace api.Entities;

public class ChatScoreSnapshot : AuditableEntity
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int Anx { get; set; }
    public int Dep { get; set; }
    public int Str { get; set; }
    public int Slp { get; set; }
    public int Soc { get; set; }
    public int Cdt { get; set; }
    public int Safe { get; set; }
    public int Eng { get; set; }

    public Chat? Chat { get; set; }

    public Dictionary<string, int> ToScoreDictionary() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ANX"] = Anx,
            ["DEP"] = Dep,
            ["STR"] = Str,
            ["SLP"] = Slp,
            ["SOC"] = Soc,
            ["CDT"] = Cdt,
            ["SAFE"] = Safe,
            ["ENG"] = Eng
        };
}
