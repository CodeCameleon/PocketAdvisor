using FluentResults;
using PocketAdvisor.Requests.Categories;
using PocketAdvisor.Responses.Categories;

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
    
    /// <summary>
    /// Deletes the specified global category asynchronously.
    /// </summary>
    /// <param name="categoryId">The identifier of the global category to delete.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> DeleteGlobalCategoryAsync(Guid categoryId);
    
    /// <summary>
    /// Deletes the specified personal category asynchronously.
    /// </summary>
    /// <param name="categoryId">The identifier of the personal category to delete.</param>
    /// <param name="userId">The identifier of the user who owns the category.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> DeletePersonalCategoryAsync(Guid categoryId, Guid userId);
    
    /// <summary>
    /// Retrieves all categories visible to the specified user asynchronously.
    /// </summary>
    /// <param name="userId">The identifier of the currently authenticated user.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// read-only list of <see cref="CategoryResponse" /> objects.
    /// </returns>
    Task<IReadOnlyList<CategoryResponse>> GetCategoriesAsync(Guid userId);
    
    /// <summary>
    /// Updates the name of the specified global category asynchronously.
    /// </summary>
    /// <param name="categoryId">The identifier of the global category to update.</param>
    /// <param name="request">The new name for the category.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> UpdateGlobalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request);
    
    /// <summary>
    /// Updates the name of the specified personal category asynchronously.
    /// </summary>
    /// <param name="categoryId">The identifier of the personal category to update.</param>
    /// <param name="request">The new name for the category.</param>
    /// <param name="userId">The identifier of the user who owns the category.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="Result" /> indicating the success or failure of the operation.
    /// </returns>
    Task<Result> UpdatePersonalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request, Guid userId);
}
