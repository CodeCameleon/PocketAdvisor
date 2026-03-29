using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="Transaction" /> entities.
/// </summary>
public sealed class TransactionRepository
    : BaseRepository<Transaction, TransactionRepository>, ITransactionRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public TransactionRepository(ILogger<TransactionRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Transactions)
    {
        
    }
    
    #endregion
}
