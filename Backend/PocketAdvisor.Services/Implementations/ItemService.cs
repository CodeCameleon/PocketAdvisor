using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using PocketAdvisor.Entities;
using PocketAdvisor.Repositories.Interfaces;
using PocketAdvisor.Requests.Items;
using PocketAdvisor.Services.Extensions;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.Services.Resources;

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
    
    #region CreateItemAsync
    
    /// <inheritdoc />
    public async Task<Result> CreateItemAsync(CreateItemRequest request, Guid userId)
    {
        Logger.LogInformation("Creating new item...");
        
        IValidator<CreateItemRequest> validator = GetValidator<CreateItemRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
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
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        Item item = new()
        {
            Name = normalizedName,
            UnitCategory = request.UnitCategory!.Value,
            UserId = userId
        };
        await ItemRepository.CreateAsync(item);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
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
            
            return Result.Fail(string.Empty);
        }
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        ItemRepository.Delete(item);
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Item '{ItemId}' deleted successfully.", itemId);
        }
        
        return Result.Ok();
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
        
        IValidator<UpdateItemNameRequest> validator = GetValidator<UpdateItemNameRequest>();
        ValidationResult validationResult = await validator.ValidateAsync(request);
        
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
            
            return Result.Fail(string.Empty);
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
        
        await TransactionManager.Value.BeginTransactionAsync();
        
        item.Name = normalizedName;
        
        await TransactionManager.Value.CommitTransactionAsync();
        
        if (Logger.IsEnabled(LogLevel.Information))
        {
            Logger.LogInformation("Item '{ItemId}' name updated successfully.", itemId);
        }
        
        return Result.Ok();
    }
    
    #endregion
}
