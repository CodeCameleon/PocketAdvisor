using FluentResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Transactions;
using PocketAdvisor.Services.Constants;
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
            if (result.Errors.Any(e => string.IsNullOrEmpty(e.Message) &&
                !e.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out _)))
            {
                return NotFound();
            }
            
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
}
