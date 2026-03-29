using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="TransactionItem" /> entities.
/// </summary>
public sealed class TransactionItemRepository
    : BaseRepository<TransactionItem, TransactionItemRepository>, ITransactionItemRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionItemRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public TransactionItemRepository(ILogger<TransactionItemRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.TransactionItems)
    {
        
    }
    
    #endregion
}
