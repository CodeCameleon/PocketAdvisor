using System.ComponentModel.DataAnnotations;

namespace PocketAdvisor.WebApplication.Configurations;

/// <summary>
/// Represents the token-related secrets bound from configuration.
/// </summary>
public sealed class TokenSecretsOptions
{
    /// <summary>
    /// The name of the configuration section that contains token secrets.
    /// </summary>
    public const string SectionName = "TokenSecrets";
    
    /// <summary>
    /// The secret used for email verification tokens.
    /// </summary>
    [Required]
    public required string EmailVerification { get; init; }
    
    /// <summary>
    /// The secret used for JSON Web Tokens (JWT).
    /// </summary>
    [Required]
    public required string JsonWeb { get; init; }
    
    /// <summary>
    /// The secret used for password reset tokens.
    /// </summary>
    [Required]
    public required string PasswordReset { get; init; }
    
    /// <summary>
    /// The secret used for refresh tokens.
    /// </summary>
    [Required]
    public required string Refresh { get; init; }
}
