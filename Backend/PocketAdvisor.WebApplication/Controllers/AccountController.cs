using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Accounts;
using PocketAdvisor.Responses.Accounts;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling account-related operations.
/// </summary>
[Authorize]
[Route("api/accounts")]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
public sealed class AccountController
    : BaseController<IAccountService>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountController" /> class.
    /// </summary>
    /// <param name="accountService">The account service instance.</param>
    public AccountController(IAccountService accountService) : base(accountService) { }
    
    #endregion
    
    #region CreateAccountAsync
    
    /// <summary>
    /// Creates a new account for the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="request">The data of the account to create.</param>
    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateAccountAsync([FromBody] CreateAccountRequest request)
    {
        Result result = await Service.CreateAccountAsync(request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
    
    #region DeleteAccountAsync
    
    /// <summary>
    /// Deletes the specified account belonging to the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the account to delete.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAccountAsync([FromRoute] Guid id)
    {
        Result result = await Service.DeleteAccountAsync(id, CurrentUserId);
        
        if (result.IsFailed)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region GetAccountsAsync
    
    /// <summary>
    /// Retrieves all accounts belonging to the currently authenticated user asynchronously.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountsAsync()
    {
        IReadOnlyList<AccountResponse> response = await Service.GetAccountsAsync(CurrentUserId);
        return Ok(response);
    }
    
    #endregion
    
    #region UpdateAccountNameAsync
    
    /// <summary>
    /// Updates the name of the specified account belonging to the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the account to update.</param>
    /// <param name="request">The new name for the account.</param>
    [HttpPatch("{id:guid}/name")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateAccountNameAsync([FromRoute] Guid id,
        [FromBody] UpdateAccountNameRequest request)
    {
        Result result = await Service.UpdateAccountNameAsync(id, request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
}
