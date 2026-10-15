namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for refreshing an expired JSON Web Token or ending a session.
/// </summary>
public sealed class RefreshRequest
{
    /// <summary>
    /// The plain-text refresh token previously issued to the user.
    /// </summary>
    public string? RefreshToken { get; set; }
}
