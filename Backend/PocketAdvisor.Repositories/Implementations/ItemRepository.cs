using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="Item" /> entities.
/// </summary>
public sealed class ItemRepository
    : BaseRepository<Item, ItemRepository>, IItemRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public ItemRepository(ILogger<ItemRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Items)
    {
        
    }
    
    #endregion
}
