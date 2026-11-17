using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the base repository implementation for performing basic CRUD operations on entities.
/// </summary>
/// <typeparam name="TEntity">The entity type that the repository will manage.</typeparam>
/// <typeparam name="TRepository">The concrete repository type that inherits from this base class.</typeparam>
public abstract partial class BaseRepository<TEntity, TRepository>
    : IBaseRepository<TEntity>
    where TEntity : class
    where TRepository : class, IBaseRepository<TEntity>
{
    #region Constants

    /// <summary>
    /// The name of the entity type, used for logging purposes.
    /// </summary>
    private static readonly string EntityName = typeof(TEntity).Name;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseRepository{TEntity,TRepository}" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    /// <param name="entities">A function that returns the <see cref="DbSet{TEntity}" /> from the context.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    protected BaseRepository(ILogger<TRepository> logger, PocketAdvisorDbContext context,
        Func<PocketAdvisorDbContext, DbSet<TEntity>> entities)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(entities);

        Entities = entities.Invoke(context);
        Logger = logger;
    }

    #endregion

    #region Properties

    /// <summary>
    /// The database table for the entities.
    /// </summary>
    private DbSet<TEntity> Entities { get; }

    /// <summary>
    /// The logger for the class.
    /// </summary>
    private ILogger<TRepository> Logger { get; }

    #endregion

    #region LogMessages

    /// <summary>
    /// Logs that an entity was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Created the {EntityName} entity.")]
    private static partial void LogEntityCreated(ILogger logger, string entityName);

    /// <summary>
    /// Logs that an entity matching the predicate exists.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "The {EntityName} entity exists.")]
    private static partial void LogEntityExists(ILogger logger, string entityName);

    /// <summary>
    /// Logs that no entity matching the predicate exists.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "The {EntityName} entity does not exist.")]
    private static partial void LogEntityDoesNotExist(ILogger logger, string entityName);

    /// <summary>
    /// Logs that an entity was found.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Found the {EntityName} entity.")]
    private static partial void LogEntityFound(ILogger logger, string entityName);

    /// <summary>
    /// Logs that an entity was not found.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "The {EntityName} entity was not found.")]
    private static partial void LogEntityNotFound(ILogger logger, string entityName);

    /// <summary>
    /// Logs the number of retrieved entities.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of retrieved entities.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Retrieved {Count} {EntityName} entities.")]
    private static partial void LogEntitiesRetrieved(ILogger logger, int count, string entityName);

    /// <summary>
    /// Logs that an entity was updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Updated the {EntityName} entity.")]
    private static partial void LogEntityUpdated(ILogger logger, string entityName);

    /// <summary>
    /// Logs that an entity was deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="entityName">The name of the entity type.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Deleted the {EntityName} entity.")]
    private static partial void LogEntityDeleted(ILogger logger, string entityName);

    #endregion

    #region CreateAsync

    /// <inheritdoc />
    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        await Entities.AddAsync(entity, cancellationToken);

        LogEntityCreated(Logger, EntityName);
    }

    #endregion

    #region ExistsAsync

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        bool exists = await Entities.AnyAsync(predicate, cancellationToken);

        if (exists)
        {
            LogEntityExists(Logger, EntityName);
        }
        else
        {
            LogEntityDoesNotExist(Logger, EntityName);
        }

        return exists;
    }

    #endregion

    #region GetSingleOrDefaultAsync

    /// <inheritdoc />
    public async Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        bool asTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        IQueryable<TEntity> query = Entities.AsQueryable();

        if (asTracking)
        {
            query = query.AsTracking();
        }

        if (includes is not null)
        {
            query = includes(query);
        }

        TEntity? entity = await query.SingleOrDefaultAsync(predicate, cancellationToken);

        if (entity is not null)
        {
            LogEntityFound(Logger, EntityName);
        }
        else
        {
            LogEntityNotFound(Logger, EntityName);
        }

        return entity;
    }

    #endregion

    #region GetAllAsync

    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
        bool asSplitQuery = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        IQueryable<TEntity> query = Entities.AsQueryable();

        if (includes is not null)
        {
            query = includes(query);
        }

        if (asSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        List<TEntity> entities = await query.Where(predicate).ToListAsync(cancellationToken);

        LogEntitiesRetrieved(Logger, entities.Count, EntityName);

        return entities.AsReadOnly();
    }

    #endregion

    #region Update

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Entities.Update(entity);

        LogEntityUpdated(Logger, EntityName);
    }

    #endregion

    #region Delete

    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        Entities.Remove(entity);

        LogEntityDeleted(Logger, EntityName);
    }

    #endregion
}
