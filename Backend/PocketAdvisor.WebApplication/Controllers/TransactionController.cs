using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
}
