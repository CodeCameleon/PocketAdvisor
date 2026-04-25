namespace PocketAdvisor.Requests.Users;

/// <summary>
/// Provides shared password validation logic for user request validators.
/// </summary>
internal static class PasswordValidator
{
    /// <summary>
    /// Validates whether the given password satisfies the required strength policy.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>
    /// <see langword="true" />, if the password is strong enough, <see langword="false" /> otherwise.
    /// </returns>
    internal static bool BeStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }
        
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));
        
        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
