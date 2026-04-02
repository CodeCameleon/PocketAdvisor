using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the base service implementation for all services.
/// </summary>
/// <typeparam name="TService">The concrete service type that inherits from this base class.</typeparam>
public abstract class BaseService<TService>
    where TService : class, IBaseService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseService{TService}" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="transactionManager">The transaction manager of the database.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    protected BaseService(ILogger<TService> logger, ITransactionManager transactionManager)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(transactionManager);
        
        Logger = logger;
        TransactionManager = transactionManager;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The logger for the class.
    /// </summary>
    protected ILogger<TService> Logger { get; }
    
    /// <summary>
    /// The transaction manager of the database.
    /// </summary>
    protected ITransactionManager TransactionManager { get; }
    
    #endregion
}
