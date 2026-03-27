namespace PocketAdvisor.Repositories.Interfaces;

/// <summary>
/// Defines the base repository interface for performing basic CRUD operations on entities.
/// </summary>
/// <typeparam name="TEntity">The entity type that the repository will manage.</typeparam>
public interface IBaseRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Creates a new entity in the database asynchronously.
    /// </summary>
    /// <param name="entity">The entity to be created in the database.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <exception cref="ArgumentNullException">
    /// If the entity parameter is <see langword="null" />.
    /// </exception>
    Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity in the database.
    /// </summary>
    /// <param name="entity">The entity to be updated in the database.</param>
    /// <exception cref="ArgumentNullException">
    /// If the entity parameter is <see langword="null" />.
    /// </exception>
    void Update(TEntity entity);
    
    /// <summary>
    /// Deletes an existing entity from the database.
    /// </summary>
    /// <param name="entity">The entity to be deleted from the database.</param>
    /// <exception cref="ArgumentNullException">
    /// If the entity parameter is <see langword="null" />.
    /// </exception>
    void Delete(TEntity entity);
}
