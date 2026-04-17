using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PocketAdvisor.Services.Constants;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// Represents the base controller implementation for all controllers.
/// </summary>
/// <typeparam name="TService">The service interface type the controller manages.</typeparam>
[ApiController]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
public abstract class BaseController<TService>
    : ControllerBase
    where TService : IBaseService
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseController{TService}" /> class.
    /// </summary>
    /// <param name="service">The service to be used by the controller.</param>
    /// <exception cref="ArgumentNullException">
    /// If the given service parameter is <see langword="null" />.
    /// </exception>
    protected BaseController(TService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        
        Service = service;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The service to be used by the controller.
    /// </summary>
    protected TService Service { get; }
    
    #endregion
    
    #region BadRequest
    
    /// <summary>
    /// Creates a <see cref="BadRequestObjectResult" /> that produces a status code 400 response.
    /// </summary>
    /// <param name="errors">The FluentResults errors to return to the client.</param>
    /// <returns>The created <see cref="BadRequestObjectResult" /> for the response.</returns>
    /// <exception cref="ArgumentNullException">
    /// If the given errors parameter is <see langword="null" />.
    /// </exception>
    protected BadRequestObjectResult BadRequest(IReadOnlyList<IError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        
        ProblemDetailsFactory problemDetailsFactory = HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>();
        ModelStateDictionary modelState = new();
        
        foreach (IError error in errors)
        {
            string key = string.Empty;
            
            if (error.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out object? propertyName))
            {
                key = propertyName.ToString()!;
            }
            
            modelState.AddModelError(key, error.Message);
        }
        
        ValidationProblemDetails validationProblemDetails = problemDetailsFactory.CreateValidationProblemDetails(
            HttpContext,
            modelState
        );
        
        return base.BadRequest(validationProblemDetails);
    }
    
    #endregion
}
