using System.Text.Json.Serialization;

namespace api.Contracts.Journals;

public record UpdateJournalRequest(
    [property: JsonPropertyName("journal_text")] string JournalText
);
