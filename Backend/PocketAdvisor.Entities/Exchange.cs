using PocketAdvisor.Enums;

namespace PocketAdvisor.Entities;

/// <summary>
/// The database entity representing an exchange rate in the system.
/// </summary>
public class Exchange
{
    /// <summary>
    /// The currency code we want to exchange.
    /// </summary>
    public required ECurrencyCode Base { get; set; }
    
    /// <summary>
    /// The currency code we want to get.
    /// </summary>
    public required ECurrencyCode Target { get; set; }
    
    /// <summary>
    /// The date of the exchange rate.
    /// </summary>
    public required DateOnly Date { get; set; }
    
    /// <summary>
    /// The rate at which the exchange is done.
    /// </summary>
    public required decimal Rate { get; set; }
}
