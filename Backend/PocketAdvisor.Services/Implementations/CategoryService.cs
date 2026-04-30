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
    
    #region DeleteGlobalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> DeleteGlobalCategoryAsync(Guid categoryId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Deleting global category '{CategoryId}'...", categoryId);
        }
        
        Category? globalCategory = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == null
        );
        
        if (globalCategory is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning("Global category '{CategoryId}' was not found.", categoryId);
            }
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool hasTransactions = await TransactionRepository.ExistsAsync(
            t => t.CategoryId == categoryId
        );
        
        if (hasTransactions)
        {
            return Result.Fail(ValidationMessages.CategoryHasTransactions);
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        CategoryRepository.Delete(globalCategory);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Global category '{CategoryId}' deleted successfully.", categoryId);
        }
        
        return Result.Ok();
    }
    
    #endregion
    
    #region DeletePersonalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> DeletePersonalCategoryAsync(Guid categoryId, Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Deleting personal category '{CategoryId}'...", categoryId);
        }
        
        Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == userId
        );
        
        if (category is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Personal category '{CategoryId}' was not found for user '{UserId}'.",
                    categoryId,
                    userId
                );
            }
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool hasTransactions = await TransactionRepository.ExistsAsync(
            t => t.CategoryId == categoryId
        );
        
        if (hasTransactions)
        {
            return Result.Fail(ValidationMessages.CategoryHasTransactions);
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        CategoryRepository.Delete(category);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Personal category '{CategoryId}' deleted successfully.", categoryId);
        }
        
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
    
    #region UpdateGlobalCategoryNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdateGlobalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Updating name of global category '{CategoryId}'...", categoryId);
        }
        
        IValidator<UpdateCategoryNameRequest> validator = GetValidator<UpdateCategoryNameRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for UpdateCategoryNameRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Category? globalCategory = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == null,
            asTracking: true
        );
        
        if (globalCategory is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning("Global category '{CategoryId}' was not found.", categoryId);
            }
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool nameExists = await CategoryRepository.ExistsAsync(
            c => c.UserId == null && c.Name == normalizedName && c.Id != categoryId
        );
        
        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.CategoryNameAlreadyExists, nameof(request.Name))
            );
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        globalCategory.Name = normalizedName;
        
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
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Global category '{CategoryId}' name updated successfully.", categoryId);
        }
        
        return Result.Ok();
    }
    
    #endregion
    
    #region UpdatePersonalCategoryNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdatePersonalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request,
        Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Updating name of personal category '{CategoryId}'...", categoryId);
        }
        
        IValidator<UpdateCategoryNameRequest> validator = GetValidator<UpdateCategoryNameRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for UpdateCategoryNameRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == userId,
            asTracking: true
        );
        
        if (category is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Personal category '{CategoryId}' was not found for user '{UserId}'.",
                    categoryId,
                    userId
                );
            }
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool nameExists = await CategoryRepository.ExistsAsync(
            c => c.Name == normalizedName && (c.UserId == null || c.UserId == userId) && c.Id != categoryId
        );
        
        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.CategoryNameAlreadyExists, nameof(request.Name))
            );
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        category.Name = normalizedName;
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Personal category '{CategoryId}' name updated successfully.", categoryId);
        }
        
        return Result.Ok();
    }
    
    #endregion
}
