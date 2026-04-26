using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
