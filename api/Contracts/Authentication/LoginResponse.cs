namespace api.Contracts.Authentication;

// Alias kept for backward compatibility – AuthResponse is the canonical type
public record LoginResponse(
    int      Id,
    string?  Email,
    string   FirstName,
    string   LastName,
    string   Token,
    int      ExpiresIn,
    string   RefreshToken,
    DateTime RefreshTokenExpiration
);
