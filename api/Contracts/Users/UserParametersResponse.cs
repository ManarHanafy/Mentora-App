namespace api.Contracts.Users;

public record UserParametersResponse(
    int UserId,
    ParameterValues Parameters,
    DateTime? UpdatedAt,
    int? LatestJournalEntryId
);

public record ParameterValues(
    int Anx,
    int Dep,
    int Str,
    int Slp,
    int Soc,
    int Cdt,
    int Safe,
    int Eng
);
