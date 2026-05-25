using api.Abstractions;

namespace api.Errors;

public static class AuthErrors
{
    public static readonly Error InvalidToken = new(
        "Auth.InvalidToken",
        "The provided token is invalid.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "Invalid email or password.",
        StatusCodes.Status401Unauthorized);
}
