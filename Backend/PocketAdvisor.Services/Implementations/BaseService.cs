using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    protected BaseService(ILogger<TService> logger, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        
        Logger = logger;
        TransactionManager = new(serviceProvider.GetRequiredService<ITransactionManager>);
        ServiceProvider = serviceProvider;
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
    protected Lazy<ITransactionManager> TransactionManager { get; }
    
    /// <summary>
    /// The service provider for resolving dependencies.
    /// </summary>
    private IServiceProvider ServiceProvider { get; }
    
    #endregion
    
    #region GetValidator
    
    /// <summary>
    /// Resolves the validator for the given request model type.
    /// </summary>
    /// <typeparam name="TRequest">The request model type.</typeparam>
    /// <returns>The validator registered for the given request model type.</returns>
    protected IValidator<TRequest> GetValidator<TRequest>()
        where TRequest : class
    {
        return ServiceProvider.GetRequiredService<IValidator<TRequest>>();
    }
    
    #endregion
}
