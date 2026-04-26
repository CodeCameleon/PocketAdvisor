using Microsoft.Extensions.Logging;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to categories.
/// </summary>
public sealed class CategoryService
    : BaseService<CategoryService>, ICategoryService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="serviceProvider">The service provider for resolving dependencies.</param>
    /// <param name="categoryRepository">The category repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public CategoryService(ILogger<CategoryService> logger, IServiceProvider serviceProvider,
        ICategoryRepository categoryRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        
        CategoryRepository = categoryRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The category repository instance.
    /// </summary>
    private ICategoryRepository CategoryRepository { get; }
    
    #endregion
}
