using System.ComponentModel.DataAnnotations;

namespace PocketAdvisor.Services.Configurations;

/// <summary>
/// Represents the JWT-related settings bound from configuration.
/// </summary>
public sealed class JsonWebTokenOptions
    : IBaseOptions
{
    /// <inheritdoc />
    public static string SectionName => "JsonWebToken";
    
    /// <summary>
    /// The issuer of the JSON Web Token.
    /// </summary>
    [Required]
    public required string Issuer { get; init; }
    
    /// <summary>
    /// The audience of the JSON Web Token.
    /// </summary>
    [Required]
    public required string Audience { get; init; }
}
