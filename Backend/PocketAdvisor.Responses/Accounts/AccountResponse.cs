using PocketAdvisor.Enums;

namespace PocketAdvisor.Responses.Accounts;

/// <summary>
/// The response model that represents an account in the system.
/// </summary>
public sealed class AccountResponse
{
    /// <summary>
    /// The unique identifier of the account.
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The name of the account.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The calculated balance of the account, derived from the starting balance
    /// plus all incoming transaction totals minus all outgoing transaction totals.
    /// </summary>
    public required decimal CalculatedBalance { get; init; }
    
    /// <summary>
    /// The currency code of the account.
    /// </summary>
    public required ECurrencyCode CurrencyCode { get; init; }
}
