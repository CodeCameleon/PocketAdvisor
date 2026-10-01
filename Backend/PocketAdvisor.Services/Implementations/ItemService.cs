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
public sealed class ItemService
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
    
    #region CreateItemAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateItemAsync(CreateItemRequest request, Guid userId)
    {
        Logger.LogInformation("Creating new item...");
        
        ValidationResult validationResult = await CreateItemRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for CreateItemRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
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
        
        Logger.LogInformation("New item created successfully.");
        return Result.Ok();
    }
    
    #endregion
    
    #region DeleteItemAsync
    
    /// <inheritdoc />
    public async Task<Result> DeleteItemAsync(Guid itemId, Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Deleting item '{ItemId}'...", itemId);
        }
        
        Item? item = await ItemRepository.GetSingleOrDefaultAsync(
            i => i.Id == itemId && i.UserId == userId
        );
        
        if (item is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Item '{ItemId}' was not found for user '{UserId}'.",
                    itemId,
                    userId
                );
            }
            
            return Result.Fail(CreateNotFoundError());
        }
        
        await TransactionManager.BeginTransactionAsync();
        
        ItemRepository.Delete(item);
        
        await TransactionManager.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Item '{ItemId}' deleted successfully.", itemId);
        }
        
        return Result.Ok();
    }
    
    #endregion
    
    #region GetItemsAsync
    
    /// <inheritdoc />
    public async Task<IReadOnlyList<ItemResponse>> GetItemsAsync(Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieving items for user '{UserId}'...", userId);
        }
        
        IReadOnlyList<Item> items = await ItemRepository.GetAllAsync(
            i => i.UserId == userId
        );
        
        List<ItemResponse> response = items.Select(i => new ItemResponse
        {
            Id = i.Id,
            Name = i.Name,
            UnitCategory = i.UnitCategory
        }).ToList();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Retrieved {Count} items for user '{UserId}'.", response.Count, userId);
        }
        
        return response;
    }
    
    #endregion
    
    #region UpdateItemNameAsync
    
    /// <inheritdoc />
    public async Task<Result> UpdateItemNameAsync(Guid itemId, UpdateItemNameRequest request, Guid userId)
    {
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Updating name of item '{ItemId}'...", itemId);
        }
        
        ValidationResult validationResult = await UpdateItemNameRequestValidator.ValidateAsync(request);
        
        if (!validationResult.IsValid)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Validation failed for UpdateItemNameRequest: {Errors}",
                    validationResult.Errors
                );
            }
            
            return Result.Fail(validationResult.Errors.ToErrorList());
        }
        
        string normalizedName = request.Name!.Trim();
        
        Item? item = await ItemRepository.GetSingleOrDefaultAsync(
            i => i.Id == itemId && i.UserId == userId,
            asTracking: true
        );
        
        if (item is null)
        {
            if (Logger.IsEnabled(LogLevel.Warning))
            {
                Logger.LogWarning(
                    "Item '{ItemId}' was not found for user '{UserId}'.",
                    itemId,
                    userId
                );
            }
            
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
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Item '{ItemId}' name updated successfully.", itemId);
        }
        
        return Result.Ok();
    }
    
    #endregion
}
