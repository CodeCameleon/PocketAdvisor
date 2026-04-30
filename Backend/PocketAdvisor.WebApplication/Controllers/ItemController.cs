using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Items;
using PocketAdvisor.Responses.Items;
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
    
    #region DeleteItemAsync
    
    /// <summary>
    /// Deletes the specified item belonging to the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the item to delete.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteItemAsync([FromRoute] Guid id)
    {
        Result result = await Service.DeleteItemAsync(id, CurrentUserId);
        
        if (result.IsFailed)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region GetItemsAsync
    
    /// <summary>
    /// Retrieves all items belonging to the currently authenticated user asynchronously.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetItemsAsync()
    {
        IReadOnlyList<ItemResponse> response = await Service.GetItemsAsync(CurrentUserId);
        return Ok(response);
    }
    
    #endregion
    
    #region UpdateItemNameAsync
    
    /// <summary>
    /// Updates the name of the specified item belonging to the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the item to update.</param>
    /// <param name="request">The new name for the item.</param>
    [HttpPatch("{id:guid}/name")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItemNameAsync([FromRoute] Guid id,
        [FromBody] UpdateItemNameRequest request)
    {
        Result result = await Service.UpdateItemNameAsync(id, request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
}
