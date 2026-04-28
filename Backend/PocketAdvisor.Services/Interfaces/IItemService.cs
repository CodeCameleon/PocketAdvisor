using FluentResults;
using PocketAdvisor.Requests.Items;
using PocketAdvisor.Responses.Items;

namespace PocketAdvisor.Services.Interfaces;

/// <summary>
/// Defines the service interface for performing operations related to items.
/// </summary>
public interface IItemService
    : IBaseService
{
    /// <summary>
    /// Creates a new item for the specified user asynchronously.
    /// </summary>
    /// <param name="request">The data of the item to create.</param>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CreateItemAsync(CreateItemRequest request, Guid userId);
    
    /// <summary>
    /// Deletes the specified item asynchronously.
    /// </summary>
    /// <param name="itemId">The identifier of the item to delete.</param>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> DeleteItemAsync(Guid itemId, Guid userId);
    
    /// <summary>
    /// Retrieves all items that belong to the specified user asynchronously.
    /// </summary>
    /// <param name="userId">The identifier of the user whose items to retrieve.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// read-only list of <see cref="ItemResponse" /> objects.
    /// </returns>
    Task<IReadOnlyList<ItemResponse>> GetItemsAsync(Guid userId);
    
    /// <summary>
    /// Updates the name of the specified item asynchronously.
    /// </summary>
    /// <param name="itemId">The identifier of the item to update.</param>
    /// <param name="request">The new name for the item.</param>
    /// <param name="userId">The identifier of the user who owns the item.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> UpdateItemNameAsync(Guid itemId, UpdateItemNameRequest request, Guid userId);
}
