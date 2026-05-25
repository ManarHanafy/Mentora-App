namespace api.Contracts.Authentication;

public record AuthResponse(
    int      Id,
    string?  Email,
    string   FirstName,
    string   LastName,
    string   Token,
    int      ExpiresIn,
    string   RefreshToken,
    DateTime RefreshTokenExpiration
);
