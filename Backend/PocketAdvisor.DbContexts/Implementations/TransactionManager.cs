using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;

namespace PocketAdvisor.DbContexts.Implementations;

/// <summary>
/// Represents the implementation for managing database transactions.
/// </summary>
public sealed partial class TransactionManager
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

    #region LogMessages

    /// <summary>
    /// Logs that a new transaction was started.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Started a new transaction with ID '{TransactionId}'.")]
    private static partial void LogTransactionStarted(ILogger logger, Guid transactionId);

    /// <summary>
    /// Logs that the transaction was committed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Committed the transaction with ID '{TransactionId}'.")]
    private static partial void LogTransactionCommitted(ILogger logger, Guid transactionId);

    /// <summary>
    /// Logs that committing the transaction failed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Error,
        Message = "An error occurred while committing the transaction with ID '{TransactionId}'.")]
    private static partial void LogTransactionCommitFailed(ILogger logger, Exception exception, Guid transactionId);

    /// <summary>
    /// Logs that the changes were saved in the transaction.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information,
        Message = "Changes saved in the transaction with ID '{TransactionId}'.")]
    private static partial void LogChangesSaved(ILogger logger, Guid transactionId);

    /// <summary>
    /// Logs that saving the changes in the transaction failed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <param name="transactionId">The ID of the transaction.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Error,
        Message = "An error occurred while saving changes in the transaction with ID '{TransactionId}'.")]
    private static partial void LogSaveChangesFailed(ILogger logger, Exception exception, Guid transactionId);

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

        LogTransactionStarted(Logger, _transaction.TransactionId);
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

            LogTransactionCommitted(Logger, _transaction.TransactionId);
        }
        catch (Exception exception)
        {
            LogTransactionCommitFailed(Logger, exception, _transaction.TransactionId);

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
            LogSaveChangesFailed(Logger, exception, _transaction.TransactionId);

            await _transaction.RollbackAsync(cancellationToken);

            await _transaction.DisposeAsync();
            _transaction = null;

            throw;
        }

        LogChangesSaved(Logger, _transaction.TransactionId);
    }

    #endregion
}
