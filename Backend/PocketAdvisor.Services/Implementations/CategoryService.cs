using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Categories;
using PocketAdvisor.Responses.Categories;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

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
    /// <param name="transactionRepository">The transaction repository instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public CategoryService(ILogger<CategoryService> logger, IServiceProvider serviceProvider,
        ICategoryRepository categoryRepository, ITransactionRepository transactionRepository)
        : base(logger, serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);
        
        CategoryRepository = categoryRepository;
        TransactionRepository = transactionRepository;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The category repository instance.
    /// </summary>
    private ICategoryRepository CategoryRepository { get; }
    
    /// <summary>
    /// The transaction repository instance.
    /// </summary>
    private ITransactionRepository TransactionRepository { get; }
    
    #endregion
    
    #region CreateGlobalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateGlobalCategoryAsync(CreateCategoryRequest request)
    {
        Logger.LogInformation("Creating new global category...");
        
        IValidator<CreateCategoryRequest> validator = GetValidator<CreateCategoryRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for CreateCategoryRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        bool globalExists = await CategoryRepository.ExistsAsync(
            c => c.UserId == null && c.Name == normalizedName
        );
        
        if (globalExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.CategoryNameAlreadyExists, nameof(request.Name))
            );
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        Category globalCategory = new()
        {
            Name = normalizedName,
            UserId = null
        };
        await CategoryRepository.CreateAsync(globalCategory);
        
        await TransactionManager.Value.SaveChangesAsync();
        
        IReadOnlyList<Category> personalCategories = await CategoryRepository.GetAllAsync(
            c => c.UserId != null && c.Name == normalizedName
        );
        
        foreach (Category personalCategory in personalCategories)
        {
            IReadOnlyList<Transaction> transactions = await TransactionRepository.GetAllAsync(
                t => t.CategoryId == personalCategory.Id
            );
            
            foreach (Transaction transaction in transactions)
            {
                transaction.CategoryId = globalCategory.Id;
                TransactionRepository.Update(transaction);
            }
            
            CategoryRepository.Delete(personalCategory);
        }
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        Logger.LogInformation("New global category created successfully.");
        return Result.Ok();
    }
    
    #endregion
    
    #region CreatePersonalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> CreatePersonalCategoryAsync(CreateCategoryRequest request, Guid userId)
    {
        Logger.LogInformation("Creating new personal category...");
        
        IValidator<CreateCategoryRequest> validator = GetValidator<CreateCategoryRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for CreateCategoryRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        bool nameExists = await CategoryRepository.ExistsAsync(
            c => c.Name == normalizedName && (c.UserId == null || c.UserId == userId)
        );
        
        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.CategoryNameAlreadyExists, nameof(request.Name))
            );
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        Category category = new()
        {
            Name = normalizedName,
            UserId = userId
        };
        await CategoryRepository.CreateAsync(category);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        Logger.LogInformation("New personal category created successfully.");
        return Result.Ok();
    }
    
    #endregion
    
    #region GetCategoriesAsync
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieving categories for user '{UserId}'...", userId);
        }
        
        IReadOnlyList<Category> categories = await CategoryRepository.GetAllAsync(
            c => c.UserId == null || c.UserId == userId
        );
        
        List<CategoryResponse> response = categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name 
        }).ToList();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieved {Count} categories for user '{UserId}'.", response.Count, userId);
        }
        
        return response;
    }
    
    #endregion
}
