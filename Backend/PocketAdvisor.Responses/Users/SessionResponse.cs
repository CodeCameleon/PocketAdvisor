using System.Text.Json.Serialization;

namespace PocketAdvisor.Responses.Users;

/// <summary>
/// The response model that represents the tokens issued for an authenticated session.
/// </summary>
public sealed class SessionResponse
{
    /// <summary>
    /// The plain-text JSON Web Token to use for authenticated requests.
    /// </summary>
    public required string JsonWebToken { get; init; }
    
    /// <summary>
    /// The plain-text refresh token to use for obtaining a new JSON Web Token.
    /// </summary>
    [JsonIgnore]
    public required string RefreshToken { get; init; }
    
    /// <summary>
    /// The expiration date and time of the refresh token in UTC.
    /// </summary>
    [JsonIgnore]
    public required DateTime RefreshTokenExpiryAt { get; init; }
}
