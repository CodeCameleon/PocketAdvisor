namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for verifying a user's email address.
/// </summary>
public sealed class VerifyEmailRequest
{
    /// <summary>
    /// The plain-text email verification token sent to the user upon registration.
    /// </summary>
    public string? Token { get; set; }
}
