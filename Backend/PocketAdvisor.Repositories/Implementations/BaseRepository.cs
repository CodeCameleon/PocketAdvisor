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
    where TRepository : IBaseRepository<TEntity>
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
    /// The database table for the <see cref="TEntity" /> entities.
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
