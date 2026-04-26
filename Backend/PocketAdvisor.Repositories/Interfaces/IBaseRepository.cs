using System.Linq.Expressions;

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
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// If the entity parameter is <see langword="null" />.
    /// </exception>
    Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Determines whether any entity matches the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The expression used to filter entities.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains <see langword="true" />,
    /// if an entity is found that matches the specified predicate, <see langword="false" /> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If the predicate parameter is <see langword="null" />.
    /// </exception>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves a single entity that matches the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The expression used to filter entities.</param>
    /// <param name="asTracking">
    /// A value indicating whether the returned entity should be tracked by the context.
    /// </param>
    /// <param name="includes">The related navigation properties to include in the query.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the matching entity,
    /// or <see langword="null" /> if no match is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If the predicate parameter is <see langword="null" />.
    /// </exception>
    Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        bool asTracking = false, IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves all entities that match the specified predicate asynchronously.
    /// </summary>
    /// <param name="predicate">The expression used to filter entities.</param>
    /// <param name="includes">The related navigation properties to include in the query.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list
    /// of all entities that match the predicate.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// If the predicate parameter is <see langword="null" />.
    /// </exception>
    Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken cancellationToken = default);
    
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
