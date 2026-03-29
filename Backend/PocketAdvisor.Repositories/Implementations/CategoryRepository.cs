using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Implementations;

/// <summary>
/// Represents the repository implementation for performing operations on the <see cref="Category" /> entities.
/// </summary>
public sealed class CategoryRepository
    : BaseRepository<Category, CategoryRepository>, ICategoryRepository
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="context">The database context instance.</param>
    public CategoryRepository(ILogger<CategoryRepository> logger, PocketAdvisorDbContext context)
        : base(logger, context, c => c.Categories)
    {
        
    }
    
    #endregion
}
