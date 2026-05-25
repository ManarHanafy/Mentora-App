namespace api.Infrastructure.Email;

public static class EmailTemplateBuilder
{
    public static string BuildOtpBody(string otp, int expiryMinutes)
        => $"Your verification code is {otp}. It expires in {expiryMinutes} minutes.";

    public static string BuildPasswordResetBody(string token, int expiryMinutes)
        => $"Your password reset token is {token}. It expires in {expiryMinutes} minutes.";
}
