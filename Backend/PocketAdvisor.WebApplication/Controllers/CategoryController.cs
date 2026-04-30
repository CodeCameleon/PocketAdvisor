using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Categories;
using PocketAdvisor.Responses.Categories;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling category-related operations.
/// </summary>
[Authorize]
[Route("api/categories")]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
public sealed class CategoryController
    : BaseController<ICategoryService>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryController" /> class.
    /// </summary>
    /// <param name="categoryService">The category service instance.</param>
    public CategoryController(ICategoryService categoryService) : base(categoryService) { }
    
    #endregion
    
    #region CreateGlobalCategoryAsync
    
    /// <summary>
    /// Creates a new global category available to all users asynchronously.
    /// Requires the <c>Administrator</c> role.
    /// </summary>
    /// <param name="request">The data of the category to create.</param>
    [HttpPost("global")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateGlobalCategoryAsync([FromBody] CreateCategoryRequest request)
    {
        Result result = await Service.CreateGlobalCategoryAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
    
    #region CreatePersonalCategoryAsync
    
    /// <summary>
    /// Creates a new personal category for the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="request">The data of the category to create.</param>
    [HttpPost("personal")]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePersonalCategoryAsync([FromBody] CreateCategoryRequest request)
    {
        Result result = await Service.CreatePersonalCategoryAsync(request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
    
    #region DeleteGlobalCategoryAsync
    
    /// <summary>
    /// Deletes the specified global category asynchronously.
    /// Requires the <c>Administrator</c> role.
    /// </summary>
    /// <param name="id">The identifier of the global category to delete.</param>
    [HttpDelete("global/{id:guid}")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteGlobalCategoryAsync([FromRoute] Guid id)
    {
        Result result = await Service.DeleteGlobalCategoryAsync(id);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region DeletePersonalCategoryAsync
    
    /// <summary>
    /// Deletes the specified personal category belonging to the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the personal category to delete.</param>
    [HttpDelete("personal/{id:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePersonalCategoryAsync([FromRoute] Guid id)
    {
        Result result = await Service.DeletePersonalCategoryAsync(id, CurrentUserId);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region GetCategoriesAsync
    
    /// <summary>
    /// Retrieves all categories visible to the currently authenticated user asynchronously.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCategoriesAsync()
    {
        IReadOnlyList<CategoryResponse> response = await Service.GetCategoriesAsync(CurrentUserId);
        return Ok(response);
    }
    
    #endregion
    
    #region UpdateGlobalCategoryNameAsync
    
    /// <summary>
    /// Updates the name of the specified global category asynchronously.
    /// Requires the <c>Administrator</c> role.
    /// </summary>
    /// <param name="id">The identifier of the global category to update.</param>
    /// <param name="request">The new name for the category.</param>
    [HttpPatch("global/{id:guid}/name")]
    [Authorize(Roles = "Administrator")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateGlobalCategoryNameAsync([FromRoute] Guid id,
        [FromBody] UpdateCategoryNameRequest request)
    {
        Result result = await Service.UpdateGlobalCategoryNameAsync(id, request);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region UpdatePersonalCategoryNameAsync
    
    /// <summary>
    /// Updates the name of the specified personal category belonging to the currently authenticated user
    /// asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the personal category to update.</param>
    /// <param name="request">The new name for the category.</param>
    [HttpPatch("personal/{id:guid}/name")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePersonalCategoryNameAsync([FromRoute] Guid id,
        [FromBody] UpdateCategoryNameRequest request)
    {
        Result result = await Service.UpdatePersonalCategoryNameAsync(id, request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
}
