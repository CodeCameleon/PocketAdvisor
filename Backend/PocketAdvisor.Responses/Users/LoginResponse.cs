namespace PocketAdvisor.Responses.Users;

/// <summary>
/// The response model returned after a successful user login or session refresh.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// The plain-text JSON Web Token to use for authenticated requests.
    /// </summary>
    public required string JsonWebToken { get; init; }
}
