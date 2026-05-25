namespace api.Contracts.AI;

using System.Text.Json.Serialization;

public record MatchedItemEntryResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("intensity_0_3")] int Intensity03,
    [property: JsonPropertyName("match_text")] string MatchText
);

public record MatchedItemResponse(
    [property: JsonPropertyName("parameter")] string Parameter,
    [property: JsonPropertyName("items")] List<MatchedItemEntryResponse> Items,
    [property: JsonPropertyName("reason")] string Reason
);
