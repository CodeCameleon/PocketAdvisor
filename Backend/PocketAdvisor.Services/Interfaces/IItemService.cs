using FluentResults;
using PocketAdvisor.Requests.Items;

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
}
