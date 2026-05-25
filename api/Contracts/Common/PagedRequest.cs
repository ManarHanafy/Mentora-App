namespace api.Contracts.Common;

public record PagedRequest(
    int PageNumber = 1,
    int PageSize = 20,
    string? SortBy = null,
    string? SortDirection = null,
    string? Search = null,
    bool? IsActive = null,
    bool? EmailVerified = null
);
