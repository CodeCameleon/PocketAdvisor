namespace PocketAdvisor.DbContexts.Interfaces;

/// <summary>
/// Defines the interface for managing database transactions.
/// </summary>
public interface ITransactionManager
    : IAsyncDisposable
{
    /// <summary>
    /// Begins a new database transaction asynchronously.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// If there is already an active transaction.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// If the manager has already been disposed of.
    /// </exception>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Commits the current database transaction asynchronously.
    /// If it fails, the transaction is rolled back automatically.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// If there is no active transaction.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// If the manager has already been disposed of.
    /// </exception>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves all changes made in the current database transaction asynchronously.
    /// If it fails, the transaction is rolled back automatically.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">
    /// If there is no active transaction.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// If the manager has already been disposed of.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
