using PocketAdvisor.Enums;

namespace PocketAdvisor.Requests.Transactions;

/// <summary>
/// The nested request model for a single item within a <see cref="CreateTransactionRequest" />.
/// </summary>
public sealed class CreateTransactionItemRequest
{
    /// <summary>
    /// The identifier of the item associated with the transaction.
    /// </summary>
    public Guid? ItemId { get; set; }
    
    /// <summary>
    /// The total price of the item at the time of the transaction.
    /// </summary>
    public decimal? TotalPrice { get; set; }
    
    /// <summary>
    /// The amount of the item on the transaction.
    /// </summary>
    public decimal? Amount { get; set; }
    
    /// <summary>
    /// The unit of the item on the transaction.
    /// </summary>
    public EUnit? Unit { get; set; }
}
