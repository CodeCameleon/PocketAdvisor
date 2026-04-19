namespace PocketAdvisor.Responses.Users;

/// <summary>
/// The response model returned after a successful user login.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>
    /// The plain-text JSON Web Token to use for authenticated requests.
    /// </summary>
    public required string JsonWebToken { get; init; }
    
    /// <summary>
    /// The plain-text refresh token to use for obtaining a new JSON Web Token.
    /// </summary>
    public required string RefreshToken { get; init; }
}
