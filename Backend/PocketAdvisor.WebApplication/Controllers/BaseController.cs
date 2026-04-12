using Microsoft.AspNetCore.Mvc;
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
}
