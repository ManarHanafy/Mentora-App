using api.Abstractions;

namespace api.Errors;

public static class JournalErrors
{
    public static readonly Error NotFound = new(
        "Journal.NotFound",
        "Journal entry not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error Forbidden = new(
        "Journal.Forbidden",
        "You do not have permission to access this journal entry.",
        StatusCodes.Status403Forbidden);

    public static readonly Error AIFailure = new(
        "Journal.AIFailure",
        "The AI analysis service is currently unavailable. Please try again later.",
        StatusCodes.Status503ServiceUnavailable);
}
