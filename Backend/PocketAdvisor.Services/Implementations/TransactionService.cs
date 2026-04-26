using Microsoft.Extensions.Logging;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to transactions.
/// </summary>
public sealed class TransactionService
    : BaseService<TransactionService>, ITransactionService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="transactionItemRepository">The transaction item repository instance.</param>
    /// <param name="transactionRepository">The transaction repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public TransactionService(ILogger<TransactionService> logger, IServiceProvider serviceProvider,
        ITransactionItemRepository transactionItemRepository, ITransactionRepository transactionRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(transactionItemRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);
        
        TransactionItemRepository = transactionItemRepository;
        TransactionRepository = transactionRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The transaction item repository instance.
    /// </summary>
    private ITransactionItemRepository TransactionItemRepository { get; }
    
    /// <summary>
    /// The transaction repository instance.
    /// </summary>
    private ITransactionRepository TransactionRepository { get; }
    
    #endregion
}
