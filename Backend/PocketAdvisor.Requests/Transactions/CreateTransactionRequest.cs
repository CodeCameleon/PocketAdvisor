namespace PocketAdvisor.Requests.Transactions;

/// <summary>
/// The request model for creating a new transaction in the system.
/// </summary>
public sealed class CreateTransactionRequest
{
    /// <summary>
    /// The occurrence date and time of the transaction.
    /// </summary>
    public DateTime? OccurredAt { get; set; }
    
    /// <summary>
    /// The identifier of the category to which the transaction belongs.
    /// </summary>
    public Guid? CategoryId { get; set; }
    
    /// <summary>
    /// The identifier of the account from which the transaction originated.<br />
    /// If <see langword="null" />, the transaction is considered as an income.
    /// </summary>
    public Guid? FromAccountId { get; set; }
    
    /// <summary>
    /// The identifier of the account to which the transaction is directed.<br />
    /// If <see langword="null" />, the transaction is considered as an expense.
    /// </summary>
    public Guid? ToAccountId { get; set; }
    
    /// <summary>
    /// The items that are part of this transaction.
    /// </summary>
    public List<CreateTransactionItemRequest>? Items { get; set; }
}
