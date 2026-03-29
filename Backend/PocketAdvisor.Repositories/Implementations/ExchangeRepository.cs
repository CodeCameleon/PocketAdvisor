using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="Exchange" /> entities.
/// </summary>
public sealed class ExchangeRepository
    : BaseRepository<Exchange, ExchangeRepository>, IExchangeRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public ExchangeRepository(ILogger<ExchangeRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Exchanges)
    {
        
    }
    
    #endregion
}
