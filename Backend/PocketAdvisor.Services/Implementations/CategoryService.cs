using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;
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
public sealed partial class CategoryService
    : BaseDatabaseService<CategoryService>, ICategoryService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="transactionManager">The transaction manager of the database.</param>
    /// <param name="categoryRepository">The category repository instance.</param>
    /// <param name="transactionRepository">The transaction repository instance.</param>
    /// <param name="createCategoryRequestValidator">
    /// The validator for the <see cref="CreateCategoryRequest" /> model.
    /// </param>
    /// <param name="updateCategoryNameRequestValidator">
    /// The validator for the <see cref="UpdateCategoryNameRequest" /> model.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public CategoryService(ILogger<CategoryService> logger, ITransactionManager transactionManager,
        ICategoryRepository categoryRepository, ITransactionRepository transactionRepository,
        IValidator<CreateCategoryRequest> createCategoryRequestValidator,
        IValidator<UpdateCategoryNameRequest> updateCategoryNameRequestValidator)
        : base(logger, transactionManager)
    {
        ArgumentNullException.ThrowIfNull(categoryRepository);
        ArgumentNullException.ThrowIfNull(transactionRepository);
        ArgumentNullException.ThrowIfNull(createCategoryRequestValidator);
        ArgumentNullException.ThrowIfNull(updateCategoryNameRequestValidator);
        
        CategoryRepository = categoryRepository;
        TransactionRepository = transactionRepository;
        CreateCategoryRequestValidator = createCategoryRequestValidator;
        UpdateCategoryNameRequestValidator = updateCategoryNameRequestValidator;
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
    
    /// <summary>
    /// The validator for the <see cref="CreateCategoryRequest" /> model.
    /// </summary>
    private IValidator<CreateCategoryRequest> CreateCategoryRequestValidator { get; }
    
    /// <summary>
    /// The validator for the <see cref="UpdateCategoryNameRequest" /> model.
    /// </summary>
    private IValidator<UpdateCategoryNameRequest> UpdateCategoryNameRequestValidator { get; }
    
    #endregion
    
    #region LogMessages
    
    /// <summary>
    /// Logs that a new global category is being created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Creating new global category...")]
    private static partial void LogCreatingGlobalCategory(ILogger logger);
    
    /// <summary>
    /// Logs that the create category request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Validation failed for CreateCategoryRequest: {Errors}")]
    private static partial void LogCreateCategoryRequestInvalid(ILogger logger, IEnumerable<ValidationFailure> errors);
    
    /// <summary>
    /// Logs that the new global category was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "New global category created successfully.")]
    private static partial void LogGlobalCategoryCreated(ILogger logger);
    
    /// <summary>
    /// Logs that a new personal category is being created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Creating new personal category...")]
    private static partial void LogCreatingPersonalCategory(ILogger logger);
    
    /// <summary>
    /// Logs that the new personal category was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "New personal category created successfully.")]
    private static partial void LogPersonalCategoryCreated(ILogger logger);
    
    /// <summary>
    /// Logs that a global category is being deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Deleting global category '{CategoryId}'...")]
    private static partial void LogDeletingGlobalCategory(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the global category was not found.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Warning, Message = "Global category '{CategoryId}' was not found.")]
    private static partial void LogGlobalCategoryNotFound(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the global category was deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Information,
        Message = "Global category '{CategoryId}' deleted successfully.")]
    private static partial void LogGlobalCategoryDeleted(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that a personal category is being deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Deleting personal category '{CategoryId}'...")]
    private static partial void LogDeletingPersonalCategory(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the personal category was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 10, Level = LogLevel.Warning,
        Message = "Personal category '{CategoryId}' was not found for user '{UserId}'.")]
    private static partial void LogPersonalCategoryNotFound(ILogger logger, Guid categoryId, Guid userId);
    
    /// <summary>
    /// Logs that the personal category was deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Information,
        Message = "Personal category '{CategoryId}' deleted successfully.")]
    private static partial void LogPersonalCategoryDeleted(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the categories of the user are being retrieved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 12, Level = LogLevel.Information,
        Message = "Retrieving categories for user '{UserId}'...")]
    private static partial void LogRetrievingCategories(ILogger logger, Guid userId);
    
    /// <summary>
    /// Logs the number of retrieved categories.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of retrieved categories.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 13, Level = LogLevel.Information,
        Message = "Retrieved {Count} categories for user '{UserId}'.")]
    private static partial void LogCategoriesRetrieved(ILogger logger, int count, Guid userId);
    
    /// <summary>
    /// Logs that the name of a global category is being updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 14, Level = LogLevel.Information,
        Message = "Updating name of global category '{CategoryId}'...")]
    private static partial void LogUpdatingGlobalCategoryName(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the update category name request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 15, Level = LogLevel.Warning,
        Message = "Validation failed for UpdateCategoryNameRequest: {Errors}")]
    private static partial void LogUpdateCategoryNameRequestInvalid(ILogger logger,
        IEnumerable<ValidationFailure> errors);
    
    /// <summary>
    /// Logs that the name of the global category was updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 16, Level = LogLevel.Information,
        Message = "Global category '{CategoryId}' name updated successfully.")]
    private static partial void LogGlobalCategoryNameUpdated(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the name of a personal category is being updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 17, Level = LogLevel.Information,
        Message = "Updating name of personal category '{CategoryId}'...")]
    private static partial void LogUpdatingPersonalCategoryName(ILogger logger, Guid categoryId);
    
    /// <summary>
    /// Logs that the name of the personal category was updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="categoryId">The ID of the category.</param>
    [LoggerMessage(EventId = 18, Level = LogLevel.Information,
        Message = "Personal category '{CategoryId}' name updated successfully.")]
    private static partial void LogPersonalCategoryNameUpdated(ILogger logger, Guid categoryId);
    
    #endregion
    
    #region CreateGlobalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateGlobalCategoryAsync(CreateCategoryRequest request)
    {
        LogCreatingGlobalCategory(Logger);
        
        ValidationResult validationResult = await CreateCategoryRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            LogCreateCategoryRequestInvalid(Logger, validationResult.Errors);
            
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
        
        await TransactionManager.BeginTransactionAsync();
        
        Category globalCategory = new()
        {
            Name = normalizedName,
            UserId = null
        };
        await CategoryRepository.CreateAsync(globalCategory);
        
        await TransactionManager.SaveChangesAsync();
        
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
        
        await TransactionManager.CommitTransactionAsync();
        
        LogGlobalCategoryCreated(Logger);
        return Result.Ok();
    }
    
    #endregion
    
    #region CreatePersonalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> CreatePersonalCategoryAsync(CreateCategoryRequest request, Guid userId)
    {
        LogCreatingPersonalCategory(Logger);
        
        ValidationResult validationResult = await CreateCategoryRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            LogCreateCategoryRequestInvalid(Logger, validationResult.Errors);
            
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
        
        await TransactionManager.BeginTransactionAsync();
        
        Category category = new()
        {
            Name = normalizedName,
            UserId = userId
        };
        await CategoryRepository.CreateAsync(category);
        
        await TransactionManager.CommitTransactionAsync();
        
        LogPersonalCategoryCreated(Logger);
        return Result.Ok();
    }
    
    #endregion
    
    #region DeleteGlobalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> DeleteGlobalCategoryAsync(Guid categoryId)
    {
        LogDeletingGlobalCategory(Logger, categoryId);
        
        Category? globalCategory = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == null
        );
        
        if (globalCategory is null)
        {
            LogGlobalCategoryNotFound(Logger, categoryId);
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool hasTransactions = await TransactionRepository.ExistsAsync(
            t => t.CategoryId == categoryId
        );
        
        if (hasTransactions)
        {
            return Result.Fail(ValidationMessages.CategoryHasTransactions);
        }
        
        await TransactionManager.BeginTransactionAsync();
        
        CategoryRepository.Delete(globalCategory);
        
        await TransactionManager.CommitTransactionAsync();
        
        LogGlobalCategoryDeleted(Logger, categoryId);
        
        return Result.Ok();
    }
    
    #endregion
    
    #region DeletePersonalCategoryAsync
    
    /// <inheritdoc />
    public async Task<Result> DeletePersonalCategoryAsync(Guid categoryId, Guid userId)
    {
        LogDeletingPersonalCategory(Logger, categoryId);
        
        Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == userId
        );
        
        if (category is null)
        {
            LogPersonalCategoryNotFound(Logger, categoryId, userId);
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool hasTransactions = await TransactionRepository.ExistsAsync(
            t => t.CategoryId == categoryId
        );
        
        if (hasTransactions)
        {
            return Result.Fail(ValidationMessages.CategoryHasTransactions);
        }
        
        await TransactionManager.BeginTransactionAsync();
        
        CategoryRepository.Delete(category);
        
        await TransactionManager.CommitTransactionAsync();
        
        LogPersonalCategoryDeleted(Logger, categoryId);
        
        return Result.Ok();
    }
    
    #endregion
    
    #region GetCategoriesAsync
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(Guid userId)
    {
        LogRetrievingCategories(Logger, userId);
        
        IReadOnlyList<Category> categories = await CategoryRepository.GetAllAsync(
            c => c.UserId == null || c.UserId == userId
        );
        
        List<CategoryResponse> response = categories.Select(c => new CategoryResponse
        {
            Id = c.Id,
            Name = c.Name,
            IsGlobal = c.UserId == null
        }).ToList();
        
        LogCategoriesRetrieved(Logger, response.Count, userId);
        
        return response;
    }
    
    #endregion
    
    #region UpdateGlobalCategoryNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdateGlobalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request)
    {
        LogUpdatingGlobalCategoryName(Logger, categoryId);
        
        ValidationResult validationResult = await UpdateCategoryNameRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            LogUpdateCategoryNameRequestInvalid(Logger, validationResult.Errors);
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Category? globalCategory = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == null,
            asTracking: true
        );
        
        if (globalCategory is null)
        {
            LogGlobalCategoryNotFound(Logger, categoryId);
            
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
        
        await TransactionManager.BeginTransactionAsync();
        
        globalCategory.Name = normalizedName;
        
        await TransactionManager.SaveChangesAsync();
        
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
        
        await TransactionManager.CommitTransactionAsync();
        
        LogGlobalCategoryNameUpdated(Logger, categoryId);
        
        return Result.Ok();
    }
    
    #endregion
    
    #region UpdatePersonalCategoryNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdatePersonalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request,
        Guid userId)
    {
        LogUpdatingPersonalCategoryName(Logger, categoryId);
        
        ValidationResult validationResult = await UpdateCategoryNameRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            LogUpdateCategoryNameRequestInvalid(Logger, validationResult.Errors);
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
            c => c.Id == categoryId && c.UserId == userId,
            asTracking: true
        );
        
        if (category is null)
        {
            LogPersonalCategoryNotFound(Logger, categoryId, userId);
            
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
        
        await TransactionManager.BeginTransactionAsync();
        
        category.Name = normalizedName;
        
        await TransactionManager.CommitTransactionAsync();
        
        LogPersonalCategoryNameUpdated(Logger, categoryId);
        
        return Result.Ok();
    }
    
    #endregion
}
