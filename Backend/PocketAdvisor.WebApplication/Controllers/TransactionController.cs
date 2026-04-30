using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Transactions;
using PocketAdvisor.Responses.Transactions;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling transaction-related operations.
/// </summary>
[Authorize]
[Route("api/transactions")]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized)]
public sealed class TransactionController
    : BaseController<ITransactionService>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionController" /> class.
    /// </summary>
    /// <param name="transactionService">The transaction service instance.</param>
    public TransactionController(ITransactionService transactionService) : base(transactionService) { }
    
    #endregion
    
    #region CreateTransactionAsync
    
    /// <summary>
    /// Creates a new transaction together with its items for the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="request">The data of the transaction to create.</param>
    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTransactionAsync([FromBody] CreateTransactionRequest request)
    {
        Result result = await Service.CreateTransactionAsync(request, CurrentUserId);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
    
    #region DeleteTransactionAsync
    
    /// <summary>
    /// Deletes the specified transaction and all of its items for the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="id">The identifier of the transaction to delete.</param>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTransactionAsync([FromRoute] Guid id)
    {
        Result result = await Service.DeleteTransactionAsync(id, CurrentUserId);
        
        if (result.IsFailed)
        {
            return NotFound();
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region DeleteTransactionItemAsync
    
    /// <summary>
    /// Deletes a single item from the specified transaction for the currently authenticated user asynchronously.
    /// </summary>
    /// <param name="transactionId">The identifier of the transaction.</param>
    /// <param name="itemId">The identifier of the item to remove from the transaction.</param>
    [HttpDelete("{transactionId:guid}/items/{itemId:guid}")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(void), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTransactionItemAsync([FromRoute] Guid transactionId,
        [FromRoute] Guid itemId)
    {
        Result result = await Service.DeleteTransactionItemAsync(transactionId, itemId, CurrentUserId);
        
        if (result.IsFailed)
        {
            return HandleFailure(result);
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region GetTransactionsAsync
    
    /// <summary>
    /// Retrieves all transactions associated with the specified account for the
    /// currently authenticated user asynchronously.
    /// </summary>
    /// <param name="accountId">The identifier of the account to filter transactions by.</param>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsAsync([FromQuery] Guid accountId)
    {
        IReadOnlyList<TransactionResponse> response = await Service.GetTransactionsAsync(accountId, CurrentUserId);
        return Ok(response);
    }
    
    #endregion
}
