using api.Abstractions;

namespace api.Errors;

public static class UserErrors
{
    public static readonly Error NotFound = new(
        "User.NotFound",
        "User not found.",
        StatusCodes.Status404NotFound);

    public static readonly Error DuplicateEmail = new(
        "User.DuplicateEmail",
        "A user with this email already exists.",
        StatusCodes.Status409Conflict);

    public static readonly Error DuplicateUsername = new(
        "User.DuplicateUsername",
        "A user with this username already exists.",
        StatusCodes.Status409Conflict);

    /// <summary>Returned when email/password is wrong at login.</summary>
    public static readonly Error InvalidCredentials = new(
        "User.InvalidCredentials",
        "Invalid email or password.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidJwtToken = new(
        "User.InvalidJwtToken",
        "The provided JWT token is invalid.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error InvalidRefreshToken = new(
        "User.InvalidRefreshToken",
        "The provided refresh token is invalid or has expired.",
        StatusCodes.Status401Unauthorized);

    public static readonly Error WrongPassword = new(
        "User.WrongPassword",
        "Current password is incorrect.",
        StatusCodes.Status400BadRequest);

    public static readonly Error InvalidRole = new(
        "User.InvalidRole",
        "The provided role is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error EmailNotVerified = new(
        "User.EmailNotVerified",
        "Email address has not been verified.",
        StatusCodes.Status403Forbidden);

    public static readonly Error OtpInvalid = new(
        "User.OtpInvalid",
        "OTP code is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error OtpExpired = new(
        "User.OtpExpired",
        "OTP code has expired.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ResetTokenInvalid = new(
        "User.ResetTokenInvalid",
        "Password reset token is invalid.",
        StatusCodes.Status400BadRequest);

    public static readonly Error ResetTokenExpired = new(
        "User.ResetTokenExpired",
        "Password reset token has expired.",
        StatusCodes.Status400BadRequest);

    public static readonly Error AccountLocked = new(
        "User.AccountLocked",
        "Too many failed login attempts. Please try again later.",
        StatusCodes.Status429TooManyRequests);
}
