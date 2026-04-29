namespace PocketAdvisor.Responses.Transactions;

/// <summary>
/// The response model that represents a transaction in the system.
/// </summary>
public sealed class TransactionResponse
{
    /// <summary>
    /// The unique identifier of the transaction.
    /// </summary>
    public required Guid Id { get; set; }
    
    /// <summary>
    /// The occurrence date and time of the transaction.
    /// </summary>
    public required DateTime OccurredAt { get; set; }
    
    /// <summary>
    /// The identifier of the category to which the transaction belongs.
    /// </summary>
    public required Guid CategoryId { get; set; }
    
    /// <summary>
    /// The identifier of the account from which the transaction originated.<br />
    /// If <see langword="null" />, the transaction is considered as an income.
    /// </summary>
    public required Guid? FromAccountId { get; set; }
    
    /// <summary>
    /// The identifier of the account to which the transaction is directed.<br />
    /// If <see langword="null" />, the transaction is considered as an expense.
    /// </summary>
    public required Guid? ToAccountId { get; set; }
    
    /// <summary>
    /// The items that are part of this transaction.
    /// </summary>
    public required IReadOnlyList<TransactionItemResponse> Items { get; set; }
}
