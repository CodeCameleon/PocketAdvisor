namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for resetting a user's password using a password reset token.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>
    /// The plain-text password reset token sent to the user's email address.
    /// </summary>
    public string? Token { get; set; }
    
    /// <summary>
    /// The new password for the user.
    /// </summary>
    public string? Password { get; set; }
    
    /// <summary>
    /// The confirmation of the new password.
    /// </summary>
    public string? ConfirmPassword { get; set; }
}
