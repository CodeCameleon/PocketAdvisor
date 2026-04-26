using FluentResults;
using PocketAdvisor.Requests.Categories;

namespace PocketAdvisor.Services.Interfaces;

/// <summary>
/// Defines the service interface for performing operations related to categories.
/// </summary>
public interface ICategoryService
    : IBaseService
{
    /// <summary>
    /// Creates a new global category available to all users asynchronously.
    /// </summary>
    /// <param name="request">The data of the category to create.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CreateGlobalCategoryAsync(CreateCategoryRequest request);
    
    /// <summary>
    /// Creates a new personal category for the specified user asynchronously.
    /// </summary>
    /// <param name="request">The data of the category to create.</param>
    /// <param name="userId">The identifier of the user who owns the category.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> CreatePersonalCategoryAsync(CreateCategoryRequest request, Guid userId);
}
