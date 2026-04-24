using System.ComponentModel.DataAnnotations;

namespace PocketAdvisor.Services.Configurations;

/// <summary>
/// Represents the frontend-related settings bound from configuration.
/// </summary>
public sealed class FrontendOptions
    : IBaseOptions
{
    /// <inheritdoc />
    public static string SectionName => "Frontend";
    
    /// <summary>
    /// The base URL of the frontend application.
    /// </summary>
    [Required]
    public required string BaseUrl { get; init; }
    
    /// <summary>
    /// The path appended to <see cref="BaseUrl" /> for the email verification page.
    /// </summary>
    [Required]
    public required string EmailVerificationPath { get; init; }
    
    /// <summary>
    /// The path appended to <see cref="BaseUrl" /> for the password reset page.
    /// </summary>
    [Required]
    public required string PasswordResetPath { get; init; }
}
