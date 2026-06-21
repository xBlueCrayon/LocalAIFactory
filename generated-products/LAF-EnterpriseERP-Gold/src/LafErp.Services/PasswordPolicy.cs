namespace LafErp.Services;

/// <summary>
/// Production password policy: minimum length plus character-class requirements. Clean-room implementation
/// of widely-established complexity rules; tune via <see cref="MinLength"/> etc. for a deployment's standard.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 8;

    public static (bool Ok, string? Error) Validate(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return (false, $"Password must be at least {MinLength} characters.");
        if (!password.Any(char.IsUpper)) return (false, "Password must contain an uppercase letter.");
        if (!password.Any(char.IsLower)) return (false, "Password must contain a lowercase letter.");
        if (!password.Any(char.IsDigit)) return (false, "Password must contain a digit.");
        if (password.All(char.IsLetterOrDigit)) return (false, "Password must contain a non-alphanumeric character.");
        return (true, null);
    }

    public static bool IsStrong(string? password) => Validate(password).Ok;
}
