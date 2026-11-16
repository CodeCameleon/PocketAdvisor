using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PocketAdvisor.DbContexts.Interfaces;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Items;
using PocketAdvisor.Responses.Items;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

namespace PocketAdvisor.Services.Implementations;

/// <summary>
/// Represents the service implementation for performing operations related to items.
/// </summary>
public sealed partial class ItemService
    : BaseDatabaseService<ItemService>, IItemService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemService" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="transactionManager">The transaction manager of the database.</param>
    /// <param name="itemRepository">The item repository instance.</param>
    /// <param name="createItemRequestValidator">
    /// The validator for the <see cref="CreateItemRequest" /> model.
    /// </param>
    /// <param name="updateItemNameRequestValidator">
    /// The validator for the <see cref="UpdateItemNameRequest" /> model.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public ItemService(ILogger<ItemService> logger, ITransactionManager transactionManager,
        IItemRepository itemRepository,
        IValidator<CreateItemRequest> createItemRequestValidator,
        IValidator<UpdateItemNameRequest> updateItemNameRequestValidator)
        : base(logger, transactionManager)
    {
        ArgumentNullException.ThrowIfNull(itemRepository);
        ArgumentNullException.ThrowIfNull(createItemRequestValidator);
        ArgumentNullException.ThrowIfNull(updateItemNameRequestValidator);
        
        ItemRepository = itemRepository;
        CreateItemRequestValidator = createItemRequestValidator;
        UpdateItemNameRequestValidator = updateItemNameRequestValidator;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The item repository instance.
    /// </summary>
    private IItemRepository ItemRepository { get; }
    
    /// <summary>
    /// The validator for the <see cref="CreateItemRequest" /> model.
    /// </summary>
    private IValidator<CreateItemRequest> CreateItemRequestValidator { get; }
    
    /// <summary>
    /// The validator for the <see cref="UpdateItemNameRequest" /> model.
    /// </summary>
    private IValidator<UpdateItemNameRequest> UpdateItemNameRequestValidator { get; }
    
    #endregion
    
    #region LogMessages
    
    /// <summary>
    /// Logs that a new item is being created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Creating new item...")]
    private static partial void LogCreatingItem(ILogger logger);
    
    /// <summary>
    /// Logs that the create item request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 2, Level = LogLevel.Warning,
        Message = "Validation failed for CreateItemRequest: {Errors}")]
    private static partial void LogCreateItemRequestInvalid(ILogger logger, IEnumerable<ValidationFailure> errors);
    
    /// <summary>
    /// Logs that the new item was created.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "New item created successfully.")]
    private static partial void LogItemCreated(ILogger logger);
    
    /// <summary>
    /// Logs that an item is being deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Deleting item '{ItemId}'...")]
    private static partial void LogDeletingItem(ILogger logger, Guid itemId);
    
    /// <summary>
    /// Logs that the item was not found for the user.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 5, Level = LogLevel.Warning,
        Message = "Item '{ItemId}' was not found for user '{UserId}'.")]
    private static partial void LogItemNotFound(ILogger logger, Guid itemId, Guid userId);
    
    /// <summary>
    /// Logs that the item was deleted.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Item '{ItemId}' deleted successfully.")]
    private static partial void LogItemDeleted(ILogger logger, Guid itemId);
    
    /// <summary>
    /// Logs that the items of the user are being retrieved.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 7, Level = LogLevel.Information, Message = "Retrieving items for user '{UserId}'...")]
    private static partial void LogRetrievingItems(ILogger logger, Guid userId);
    
    /// <summary>
    /// Logs the number of retrieved items.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="count">The number of retrieved items.</param>
    /// <param name="userId">The ID of the user.</param>
    [LoggerMessage(EventId = 8, Level = LogLevel.Information, Message = "Retrieved {Count} items for user '{UserId}'.")]
    private static partial void LogItemsRetrieved(ILogger logger, int count, Guid userId);
    
    /// <summary>
    /// Logs that the name of an item is being updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Updating name of item '{ItemId}'...")]
    private static partial void LogUpdatingItemName(ILogger logger, Guid itemId);
    
    /// <summary>
    /// Logs that the update item name request failed validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="errors">The validation errors of the request.</param>
    [LoggerMessage(EventId = 10, Level = LogLevel.Warning,
        Message = "Validation failed for UpdateItemNameRequest: {Errors}")]
    private static partial void LogUpdateItemNameRequestInvalid(ILogger logger, IEnumerable<ValidationFailure> errors);
    
    /// <summary>
    /// Logs that the name of the item was updated.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="itemId">The ID of the item.</param>
    [LoggerMessage(EventId = 11, Level = LogLevel.Information, Message = "Item '{ItemId}' name updated successfully.")]
    private static partial void LogItemNameUpdated(ILogger logger, Guid itemId);
    
    #endregion
    
    #region CreateItemAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateItemAsync(CreateItemRequest request, Guid userId)
    {
        LogCreatingItem(Logger);
        
        ValidationResult validationResult = await CreateItemRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            LogCreateItemRequestInvalid(Logger, validationResult.Errors);
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        bool nameExists = await ItemRepository.ExistsAsync(
            i => i.UserId == userId && i.Name == normalizedName
        );
        
        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.ItemNameAlreadyExists, nameof(request.Name))
            );
        }
        
        await TransactionManager.BeginTransactionAsync();
        
        Item item = new()
        {
            Name = normalizedName,
            UnitCategory = request.UnitCategory!.Value,
            UserId = userId
        };
        await ItemRepository.CreateAsync(item);
        
        await TransactionManager.CommitTransactionAsync();
        
        LogItemCreated(Logger);
        return Result.Ok();
    }
    
    #endregion
    
    #region DeleteItemAsync
    
    /// <inheritdoc />
    public async Task<Result> DeleteItemAsync(Guid itemId, Guid userId)
    {
        LogDeletingItem(Logger, itemId);
        
        Item? item = await ItemRepository.GetSingleOrDefaultAsync(
            i => i.Id == itemId && i.UserId == userId
        );
        
        if (item is null)
        {
            LogItemNotFound(Logger, itemId, userId);
            
            return Result.Fail(CreateNotFoundError());
        }
        
        await TransactionManager.BeginTransactionAsync();
        
        ItemRepository.Delete(item);
        
        await TransactionManager.CommitTransactionAsync();
        
        LogItemDeleted(Logger, itemId);
        
        return Result.Ok();
    }
    
    #endregion
    
    #region GetItemsAsync
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemResponse>> GetItemsAsync(Guid userId)
    {
        LogRetrievingItems(Logger, userId);
        
        IReadOnlyList<Item> items = await ItemRepository.GetAllAsync(
            i => i.UserId == userId
        );
        
        List<ItemResponse> response = items.Select(i => new ItemResponse
        {
            Id = i.Id,
            Name = i.Name,
            UnitCategory = i.UnitCategory
        }).ToList();
        
        LogItemsRetrieved(Logger, response.Count, userId);
        
        return response;
    }
    
    #endregion
    
    #region UpdateItemNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdateItemNameAsync(Guid itemId, UpdateItemNameRequest request, Guid userId)
    {
        LogUpdatingItemName(Logger, itemId);
        
        ValidationResult validationResult = await UpdateItemNameRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            LogUpdateItemNameRequestInvalid(Logger, validationResult.Errors);
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Item? item = await ItemRepository.GetSingleOrDefaultAsync(
            i => i.Id == itemId && i.UserId == userId,
            asTracking: true
        );
        
        if (item is null)
        {
            LogItemNotFound(Logger, itemId, userId);
            
            return Result.Fail(CreateNotFoundError());
        }
        
        bool nameExists = await ItemRepository.ExistsAsync(
            i => i.UserId == userId && i.Name == normalizedName && i.Id != itemId
        );
        
        if (nameExists)
        {
            return Result.Fail(
                CreateError(ValidationMessages.ItemNameAlreadyExists, nameof(request.Name))
            );
        }
        
        await TransactionManager.BeginTransactionAsync();
        
        item.Name = normalizedName;
        
        await TransactionManager.CommitTransactionAsync();
        
        LogItemNameUpdated(Logger, itemId);
        
        return Result.Ok();
    }
    
    #endregion
}
