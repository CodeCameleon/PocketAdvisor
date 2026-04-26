using Microsoft.Extensions.Logging;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to items.
/// </summary>
public sealed class ItemService
    : BaseService<ItemService>, IItemService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="itemRepository">The item repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public ItemService(ILogger<ItemService> logger, IServiceProvider serviceProvider,
        IItemRepository itemRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(itemRepository);
        
        ItemRepository = itemRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The item repository instance.
    /// </summary>
    private IItemRepository ItemRepository { get; }
    
    #endregion
}
