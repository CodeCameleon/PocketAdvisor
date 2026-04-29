using PocketAdvisor.Enums;

namespace PocketAdvisor.Responses.Transactions;

/// <summary>
/// The nested response model for a single item within a <see cref="TransactionResponse" />.
/// </summary>
public sealed class TransactionItemResponse
{
    /// <summary>
    /// The identifier of the item associated with the transaction.
    /// </summary>
    public required Guid ItemId { get; set; }
    
    /// <summary>
    /// The total price of the item at the time of the transaction.
    /// </summary>
    public required decimal TotalPrice { get; set; }
    
    /// <summary>
    /// The amount of the item on the transaction.
    /// </summary>
    public required decimal AmountValue { get; set; }
    
    /// <summary>
    /// The unit of the item on the transaction.
    /// </summary>
    public required EUnit AmountUnit { get; set; }
}
