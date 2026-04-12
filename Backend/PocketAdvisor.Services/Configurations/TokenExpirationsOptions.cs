using System.ComponentModel.DataAnnotations;

namespace PocketAdvisor.Services.Configurations;

/// <summary>
/// Represents the token-related expirations bound from configuration.
/// </summary>
public sealed class TokenExpirationsOptions
{
    /// <summary>
    /// The name of the configuration section that contains token expirations.
    /// </summary>
    public const string SectionName = "TokenExpirations";
    
    /// <summary>
    /// The expiration in hours for email verification tokens.
    /// </summary>
    [Required]
    public required int EmailVerificationHours { get; init; }
    
    /// <summary>
    /// The expiration in minutes for JSON Web Tokens (JWT).
    /// </summary>
    [Required]
    public required int JsonWebMinutes { get; init; }
    
    /// <summary>
    /// The expiration in minutes for password reset tokens.
    /// </summary>
    [Required]
    public required int PasswordResetMinutes { get; init; }
    
    /// <summary>
    /// The expiration in days for refresh tokens.
    /// </summary>
    [Required]
    public required int RefreshDays { get; init; }
}
