using System.ComponentModel.DataAnnotations;
using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing an account in the system.
/// </summary>
public class Account
{
    /// <summary>
    /// The unique identifier of the account.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The name of the account.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }
    
    /// <summary>
    /// The starting balance of the account.
    /// </summary>
    public required decimal Balance { get; set; }
    
    /// <summary>
    /// The currency code of the account.
    /// </summary>
    public required ECurrencyCode CurrencyCode { get; set; }
    
    /// <summary>
    /// The identifier of the user, to whom the account belongs.
    /// </summary>
    public required Guid UserId { get; set; }
    
    /// <summary>
    /// The navigational property of the user, to whom the account belongs.
    /// </summary>
    public virtual User? User { get; set; }
    
    /// <summary>
    /// The navigational property of the transactions, which are incoming to the account.
    /// </summary>
    public virtual List<Transaction>? IncomingTransactions { get; set; }
    
    /// <summary>
    /// The navigational property of the transactions, which are outgoing from the account.
    /// </summary>
    public virtual List<Transaction>? OutgoingTransactions { get; set; }
}
