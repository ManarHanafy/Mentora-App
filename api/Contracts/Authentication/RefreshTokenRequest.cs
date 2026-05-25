namespace api.Contracts.Authentication;

public record RefreshTokenRequest(string Token, string RefreshToken);
