using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;

namespace PocketAdvisor.DbContexts.Implementations;

/// <summary>
/// Represents the implementation for managing database transactions.
/// </summary>
public sealed class TransactionManager
    : ITransactionManager
{
    #region Constants
    
    /// <summary>
    /// When a new transaction cannot be started.
    /// </summary>
    private const string AlreadyActiveMessage = "There is already an active transaction.";
    
    /// <summary>
    /// When there is no active transaction to work with.
    /// </summary>
    private const string NoActiveMessage = "There is no active transaction.";
    
    #endregion
    
    #region Fields
    
    /// <summary>
    /// The value indicating whether the instance has been disposed of.
    /// </summary>
    private bool _disposed;
    
    /// <summary>
    /// The current database transaction.
    /// </summary>
    private IDbContextTransaction? _transaction;
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionManager" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public TransactionManager(ILogger<TransactionManager> logger, PocketAdvisorDbContext context)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(context);
        
        Context = context;
        Logger = logger;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The database context instance.
    /// </summary>
    private PocketAdvisorDbContext Context { get; }
    
    /// <summary>
    /// The logger for the class.
    /// </summary>
    private ILogger<TransactionManager> Logger { get; }
    
    #endregion
    
    #region BeginTransactionAsync
    
    /// <inheritdoc />
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TransactionManager));
        
        if (_transaction is not null)
        {
            throw new InvalidOperationException(AlreadyActiveMessage);
        }
        
        _transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Started a new transaction with ID '{TransactionId}'.",
                _transaction.TransactionId
            );
        }
    }
    
    #endregion
    
    #region CommitTransactionAsync
    
    /// <inheritdoc />
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TransactionManager));
        
        if (_transaction is null)
        {
            throw new InvalidOperationException(NoActiveMessage);
        }
        
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
            
            if (Logger.IsEnabled(LogLevel.Information))
            {
                Logger.LogInformation(
                    "Committed the transaction with ID '{TransactionId}'.",
                    _transaction.TransactionId
                );
            }
        }
        catch (Exception exception)
        {
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(
                    exception,
                    "An error occurred while committing the transaction with ID '{TransactionId}'.",
                    _transaction.TransactionId
                );
            }
            
            await _transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
    
    #endregion
    
    #region DisposeAsync
    
    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
        
        _disposed = true;
    }
    
    #endregion
    
    #region SaveChangesAsync
    
    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(TransactionManager));
        
        if (_transaction is null)
        {
            throw new InvalidOperationException(NoActiveMessage);
        }
        
        try
        {
            await Context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            if (Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(
                    exception,
                    "An error occurred while saving changes in the transaction with ID '{TransactionId}'.",
                    _transaction.TransactionId
                );
            }
            
            await _transaction.RollbackAsync(cancellationToken);
            
            await _transaction.DisposeAsync();
            _transaction = null;
            
            throw;
        }
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation(
                "Changes saved in the transaction with ID '{TransactionId}'.",
                _transaction.TransactionId
            );
        }
    }
    
    #endregion
}
