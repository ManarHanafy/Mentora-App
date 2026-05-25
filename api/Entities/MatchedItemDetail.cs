namespace api.Entities;

public class MatchedItemDetail
{
    public int Id { get; set; }
    public int MatchedItemId { get; set; }
    public string ItemId { get; set; } = string.Empty;
    public int Intensity { get; set; }
    public string MatchText { get; set; } = string.Empty;

    public MatchedItem? MatchedItem { get; set; }
}
