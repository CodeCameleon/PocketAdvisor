using PocketAdvisor.Enums;

namespace PocketAdvisor.Requests.Accounts;

/// <summary>
/// The request model for creating a new account in the system.
/// </summary>
public sealed class CreateAccountRequest
{
    /// <summary>
    /// The name of the account.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The starting balance of the account.
    /// </summary>
    public decimal? Balance { get; set; }
    
    /// <summary>
    /// The currency code of the account.
    /// </summary>
    public ECurrencyCode? CurrencyCode { get; set; }
}
