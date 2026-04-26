using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
