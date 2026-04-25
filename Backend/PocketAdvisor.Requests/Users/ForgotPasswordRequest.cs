namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for initiating a password reset by sending a reset email.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>
    /// The email address of the user requesting a password reset.
    /// </summary>
    public string? Email { get; set; }
}
