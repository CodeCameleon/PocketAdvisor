using Microsoft.EntityFrameworkCore;
using PocketAdvisor.Entities.ValueObjects;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing a connection between a transaction and an item in the system.
/// </summary>
public class TransactionItem
{
    /// <summary>
    /// The identifier of the transaction on which the item appears.
    /// </summary>
    public required Guid TransactionId { get; set; }
    
    /// <summary>
    /// The identifier of the item that is associated with the transaction.
    /// </summary>
    public required Guid ItemId { get; set; }
    
    /// <summary>
    /// The total price of the item at the time of the transaction.
    /// </summary>
    [Precision(18, 2)]
    public required decimal TotalPrice { get; set; }
    
    /// <summary>
    /// The amount of the item on the transaction.
    /// </summary>
    public required Quantity Amount { get; set; }
    
    /// <summary>
    /// The navigational property of the transaction on which the item appears.
    /// </summary>
    public virtual Transaction? Transaction { get; set; }
    
    /// <summary>
    /// The navigational property of the item that is associated with the transaction.
    /// </summary>
    public virtual Item? Item { get; set; }
}
