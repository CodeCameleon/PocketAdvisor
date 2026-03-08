using System.ComponentModel.DataAnnotations;
using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing a token in the system.
/// </summary>
public class Token
{
    /// <summary>
    /// The unique identifier of the token.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The hashed value of the token.
    /// </summary>
    [Required]
    [MaxLength(44)]
    public required string Hash { get; set; }
    
    /// <summary>
    /// The expiration date and time of the token.
    /// </summary>
    public required DateTime ExpiryAt { get; set; }
    
    /// <summary>
    /// The type of the token.
    /// </summary>
    public required ETokenType Type { get; set; }
    
    /// <summary>
    /// The identifier of the user, to whom the token belongs.
    /// </summary>
    public required Guid UserId { get; set; }
    
    /// <summary>
    /// The navigational property of the user, to whom the token belongs.
    /// </summary>
    public virtual User? User { get; set; }
}
