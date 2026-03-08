namespace PocketAdvisor.Enums;

/// <summary>
/// The enumeration containing the possible token types in the system.
/// </summary>
public enum ETokenType
{
    /// <summary>
    /// The email verification token type.
    /// </summary>
    EmailVerification = 1,
    
    /// <summary>
    /// The password reset token type.
    /// </summary>
    PasswordReset = 2,
    
    /// <summary>
    /// The refresh token type.
    /// </summary>
    Refresh = 3
}
