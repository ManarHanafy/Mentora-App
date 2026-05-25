namespace api.Contracts.Users;

public record UserResponse(
    int Id,
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    bool EmailVerified,
    DateTime? EmailVerifiedAt,
    DateTime CreatedAt,
    int TotalJournalEntries,
    Dictionary<string, int>? Parameters,
    DateTime? ParametersUpdatedAt
);
