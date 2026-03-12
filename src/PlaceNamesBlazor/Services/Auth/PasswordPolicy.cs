namespace PlaceNamesBlazor.Services.Auth;

/// <summary>Enforces password policy: min length 8, at least one uppercase, one lowercase, one digit.</summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public static (bool IsValid, string? ErrorMessage) Validate(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return (false, "Password is required.");
        if (password.Length < MinLength)
            return (false, $"Password must be at least {MinLength} characters.");
        if (!password.Any(char.IsUpper))
            return (false, "Password must contain at least one uppercase letter.");
        if (!password.Any(char.IsLower))
            return (false, "Password must contain at least one lowercase letter.");
        if (!password.Any(char.IsDigit))
            return (false, "Password must contain at least one digit.");
        return (true, null);
    }
}
