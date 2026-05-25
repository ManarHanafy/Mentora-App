using Microsoft.AspNetCore.Authorization;
using api.Contracts.Authentication;
using api.Contracts.Users;
using api.Errors;
using api.Extensions;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, IUserService userService) : ControllerBase
{
    // ── POST /api/auth/login ──────────────────────────────────────────────────

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.GetTokenAsync(request.Email, request.Password, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        var (result, error) = await userService.CreateAsync(request, cancellationToken);
        if (error is not null)
            return string.Equals(error, UserErrors.DuplicateEmail.Description, StringComparison.Ordinal)
                ? Conflict(new { error })
                : BadRequest(new { error });

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EmailOtpStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.VerifyEmailOtpAsync(request.Email, request.Otp, cancellationToken);
        return result.IsSuccess ? Ok(new EmailOtpStatusResponse(true)) : result.ToProblem();
    }

    [HttpPost("resend-otp")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EmailOtpStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResendOtp([FromBody] ResendEmailOtpRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResendEmailOtpAsync(request.Email, cancellationToken);
        return result.IsSuccess ? Ok(new EmailOtpStatusResponse(true)) : result.ToProblem();
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RequestPasswordResetAsync(request.Email, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var result = await authService.LogoutAsync(userId.Value, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ── POST /api/auth/refresh ────────────────────────────────────────────────

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.GetRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : result.ToProblem();
    }

    // ── POST /api/auth/revoke-refresh-token ───────────────────────────────────

    [HttpPost("revoke-refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.RevokeRefreshTokenAsync(request.Token, request.RefreshToken, cancellationToken);
        return result.IsSuccess ? Ok() : result.ToProblem();
    }

    // ── GET /api/auth/status ──────────────────────────────────────────────────

    [HttpGet("status")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var user = await userService.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return NotFound(new { error = $"User {userId} not found." });

        return Ok(new
        {
            isAuthenticated = User.Identity?.IsAuthenticated ?? false,
            userId = user.Id,
            email = user.Email,
            role = user.Role,
            timestamp = DateTime.UtcNow
        });
    }

    // ── GET /api/auth/me ──────────────────────────────────────────────────────

    /// <summary>Returns lightweight identity claims from the current JWT without a database lookup.</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId is null)
            return Unauthorized(new { error = "Invalid token claims." });

        var user = await userService.GetByIdAsync(userId.Value, cancellationToken);
        if (user is null)
            return NotFound(new { error = $"User {userId} not found." });

        return Ok(new
        {
            userId = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            role = user.Role,
            permissions = api.Authorization.ApplicationPermissions.GetByRole(user.Role),
            isAuthenticated = true
        });
    }
}
