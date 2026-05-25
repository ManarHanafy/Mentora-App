using System.Text.Json.Serialization;

namespace api.Contracts.Journals;

public record SubmitJournalRequest(
    [property: JsonPropertyName("journal_text")] string JournalText
);
