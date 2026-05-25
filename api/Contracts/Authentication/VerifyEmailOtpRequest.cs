namespace api.Contracts.Authentication;

public record VerifyEmailOtpRequest(string Email, string Otp);
