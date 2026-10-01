using FluentResults;
using Microsoft.Extensions.Logging;
using PocketAdvisor.Services.Constants;
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
    /// <exception cref="ArgumentNullException">
    /// If the given logger parameter is <see langword="null" />.
    /// </exception>
    protected BaseService(ILogger<TService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        
        Logger = logger;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The logger for the class.
    /// </summary>
    protected ILogger<TService> Logger { get; }
    
    #endregion
    
    #region CreateConflictError
    
    /// <summary>
    /// Creates a new conflict <see cref="Error" /> that can be returned in a <see cref="Result" /> object.
    /// </summary>
    /// <returns>The constructed conflict <see cref="Error" /> ready to be returned.</returns>
    protected static Error CreateConflictError()
    {
        Error error = new(string.Empty)
        {
            Metadata =
            {
                [ErrorMetadataKeys.Conflict] = true
            }
        };
        
        return error;
    }
    
    #endregion
    
    #region CreateNotFoundError
    
    /// <summary>
    /// Creates a new not found <see cref="Error" /> that can be returned in a <see cref="Result" /> object.
    /// </summary>
    /// <returns>The constructed not found <see cref="Error" /> ready to be returned.</returns>
    protected static Error CreateNotFoundError()
    {
        Error error = new(string.Empty)
        {
            Metadata =
            {
                [ErrorMetadataKeys.NotFound] = true
            }
        };
        
        return error;
    }
    
    #endregion
    
    #region CreateError
    
    /// <summary>
    /// Creates a new <see cref="Error" /> that can be returned in a <see cref="Result" /> object.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="propertyName">The name of the property the error message belongs to.</param>
    /// <returns>The constructed <see cref="Error" /> ready to be returned.</returns>
    protected static Error CreateError(string message, string propertyName)
    {
        Error error = new(message)
        {
            Metadata =
            {
                [ErrorMetadataKeys.PropertyName] = propertyName
            }
        };
        
        return error;
    }
    
    #endregion
}
