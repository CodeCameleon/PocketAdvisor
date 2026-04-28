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
}
