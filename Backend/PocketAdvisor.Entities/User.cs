using System.ComponentModel.DataAnnotations;
using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing a user in the system.
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier of the user.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The flag indicating whether the user's email address has been verified.
    /// </summary>
    public bool IsEmailVerified { get; set; }
    
    /// <summary>
    /// The email address of the user.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Email { get; set; }
    
    /// <summary>
    /// The hashed password of the user.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public required string PasswordHash { get; set; }
    
    /// <summary>
    /// The role of the user.
    /// </summary>
    public required EUserRole Role { get; set; }
    
    /// <summary>
    /// The navigational property of the accounts, which belong to the user.
    /// </summary>
    public virtual List<Account>? Accounts { get; set; }
    
    /// <summary>
    /// The navigational property of the categories, which belong to the user.
    /// </summary>
    public virtual List<Category>? Categories { get; set; }
    
    /// <summary>
    /// The navigational property of the tokens, which belong to the user.
    /// </summary>
    public virtual List<Token>? Tokens { get; set; }
}
