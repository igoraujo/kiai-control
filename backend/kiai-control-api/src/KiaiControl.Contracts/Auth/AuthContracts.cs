namespace KiaiControl.Contracts.Auth;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public sealed class ForgotPasswordResponse
{
    public string Message { get; init; } = string.Empty;

    public string? ResetToken { get; init; }

    public DateTimeOffset? ResetTokenExpiresAt { get; init; }
}

public sealed class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

public sealed class AuthUserProfileResponse
{
    public Guid UserId { get; init; }

    public Guid PersonId { get; init; }

    public Guid OrganizationId { get; init; }

    public string Email { get; init; } = string.Empty;
}

public sealed class SessionTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAt { get; init; }

    public string RefreshToken { get; init; } = string.Empty;

    public DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public AuthUserProfileResponse User { get; init; } = new();
}

public sealed class SimpleMessageResponse
{
    public string Message { get; init; } = string.Empty;
}
