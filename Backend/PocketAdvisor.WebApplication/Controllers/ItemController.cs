using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Items;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling item-related operations.
/// </summary>
[Authorize]
[Route("api/items")]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
public sealed class ItemController
    : BaseController<IItemService>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ItemController" /> class.
    /// </summary>
    /// <param name="itemService">The item service instance.</param>
    public ItemController(IItemService itemService) : base(itemService) { }
    
    #endregion
    
    #region CreateItemAsync
    
    /// <summary>
    /// Creates a new item for the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="request">The data of the item to create.</param>
    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateItemAsync([FromBody] CreateItemRequest request)
    {
        Result result = await Service.CreateItemAsync(request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
}
