using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Authentication;
using api.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace api.Tests.Authentication;

public class JwtProviderTests
{
    private static readonly JwtOptions JwtSettings = new()
    {
        Key = "12345678901234567890123456789012",
        Issuer = "MentalHealthApi",
        Audience = "MentalHealthClient",
        ExpirationMinutes = 60
    };

    private readonly JwtProvider _sut = new(Options.Create(JwtSettings));

    [Fact]
    public void ValidateToken_ValidToken_ReturnsUserId()
    {
        var user = new User
        {
            Id = 42,
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
            Role = "User"
        };

        var (token, _) = _sut.GenerateToken(user);

        var result = _sut.ValidateToken(token);

        result.Should().Be("42");
    }

    [Fact]
    public void ValidateToken_WrongAudience_ReturnsNull()
    {
        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            issuer: JwtSettings.Issuer,
            audience: "DifferentAudience",
            claims: [new Claim(JwtRegisteredClaimNames.Sub, "42")],
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSettings.Key)),
                SecurityAlgorithms.HmacSha256)));

        var result = _sut.ValidateToken(token);

        result.Should().BeNull();
    }
}
