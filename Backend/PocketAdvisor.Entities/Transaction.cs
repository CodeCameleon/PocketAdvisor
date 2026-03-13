using System.ComponentModel.DataAnnotations;
using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing a transaction in the system.
/// </summary>
public class Transaction
{
    /// <summary>
    /// The unique identifier of the transaction.
    /// </summary>
    [Key]
    public Guid Id { get; set; }
    
    /// <summary>
    /// The occurrence date and time of the transaction.
    /// </summary>
    public required DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// The currency code of the transaction.
    /// </summary>
    public required ECurrencyCode CurrencyCode { get; set; }
    
    /// <summary>
    /// The identifier of the category to which the transaction belongs.
    /// </summary>
    public required Guid CategoryId { get; set; }
    
    /// <summary>
    /// The identifier of the account from which the transaction originated.<br />
    /// If <see langword="null" />, the transaction is considered as an income.
    /// </summary>
    public required Guid? FromAccountId  { get; set; }
    
    /// <summary>
    /// The identifier of the account to which the transaction is directed.<br />
    /// If <see langword="null" />, the transaction is considered as an expense.
    /// </summary>
    public required Guid? ToAccountId  { get; set; }
    
    /// <summary>
    /// The navigational property of the category to which the transaction belongs.
    /// </summary>
    public virtual Category? Category { get; set; }
    
    /// <summary>
    /// The navigational property of the account from which the transaction originated.
    /// </summary>
    public virtual Account? FromAccount { get; set; }
    
    /// <summary>
    /// The navigational property of the account to which the transaction is directed.
    /// </summary>
    public virtual Account? ToAccount { get; set; }
}
