using System.ComponentModel.DataAnnotations;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing a category in the system.
/// </summary>
public class Category
{
    /// <summary>
    /// The unique identifier of the category.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The name of the category.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    
    /// <summary>
    /// The identifier of the user, to whom the category belongs.<br />
    /// If <see langword="null" />, the category is global and can be used by all users.
    /// </summary>
    public required Guid? UserId { get; set; }
    
    /// <summary>
    /// The navigational property of the user, to whom the category belongs.
    /// </summary>
    public virtual User? User { get; set; }
    
    /// <summary>
    /// The navigational property of the transactions, which belong to the category.
    /// </summary>
    public virtual List<Transaction>? Transactions { get; set; }
}
