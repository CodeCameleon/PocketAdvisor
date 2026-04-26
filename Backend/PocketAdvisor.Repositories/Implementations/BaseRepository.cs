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
public abstract class BaseRepository<TEntity, TRepository>
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
    
    #region CreateAsync
    
    /// <inheritdoc />
    public async Task CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        await Entities.AddAsync(entity, cancellationToken);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Created the {EntityName} entity.", EntityName);
        }
    }
    
    #endregion
    
    #region ExistsAsync
    
    /// <inheritdoc />
    public async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        
        bool exists = await Entities.AnyAsync(predicate, cancellationToken);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            if (exists)
            {
                Logger.LogInformation("The {EntityName} entity exists.", EntityName);
            }
            else
            {
                Logger.LogInformation("The {EntityName} entity does not exist.", EntityName);
            }
        }
        
        return exists;
    }
    
    #endregion
    
    #region GetSingleOrDefaultAsync
    
    /// <inheritdoc />
    public async Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
        bool asTracking = false, IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
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
            query = includes.Aggregate(
                query,
                (current, include) => current.Include(include)
            );
        }
        
        TEntity? entity = await query.SingleOrDefaultAsync(predicate, cancellationToken);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            if (entity is not null)
            {
                Logger.LogInformation("Found the {EntityName} entity.", EntityName);
            }
            else
            {
                Logger.LogInformation("The {EntityName} entity was not found.", EntityName);
            }
        }
        
        return entity;
    }
    
    #endregion
    
    #region GetAllAsync
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
        IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        
        IQueryable<TEntity> query = Entities.AsQueryable();
        
        if (includes is not null)
        {
            query = includes.Aggregate(
                query,
                (current, include) => current.Include(include)
            );
        }
        
        List<TEntity> entities = await query.Where(predicate).ToListAsync(cancellationToken);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieved {Count} {EntityName} entities.", entities.Count, EntityName);
        }
        
        return entities.AsReadOnly();
    }
    
    #endregion
    
    #region Update
    
    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        Entities.Update(entity);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Updated the {EntityName} entity.", EntityName);
        }
    }
    
    #endregion
    
    #region Delete
    
    /// <inheritdoc />
    public void Delete(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        Entities.Remove(entity);
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Deleted the {EntityName} entity.", EntityName);
        }
    }
    
    #endregion
}
