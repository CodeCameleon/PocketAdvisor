using System.ComponentModel.DataAnnotations;
using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing an item in the system.
/// </summary>
public class Item
{
    /// <summary>
    /// The unique identifier of the item.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The name of the item.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    
    /// <summary>
    /// The unit category of the item.
    /// </summary>
    public required EUnitCategory UnitCategory { get; set; }
    
    /// <summary>
    /// The identifier of the user, to whom the item belongs.
    /// </summary>
    public required Guid UserId { get; set; }
    
    /// <summary>
    /// The navigational property of the user, to whom the item belongs.
    /// </summary>
    public virtual User? User { get; set; }
    
    /// <summary>
    /// The navigational property of the connection entities linking this item to its transactions.
    /// </summary>
    public virtual List<TransactionItem>? TransactionItems { get; set; }
}
