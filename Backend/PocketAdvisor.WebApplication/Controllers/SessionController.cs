using FluentResults;
using Microsoft.AspNetCore.Mvc;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Responses.Users;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling session-related operations.
/// </summary>
[Route("api/sessions")]
public sealed class SessionController
    : BaseController<IUserService>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionController" /> class.
    /// </summary>
    /// <param name="userService">The user service instance.</param>
    public SessionController(IUserService userService) : base(userService) { }
    
    #endregion
    
    #region LoginAsync
    
    /// <summary>
    /// Authenticates a user and issues a JSON Web Token and refresh token asynchronously.
    /// </summary>
    /// <param name="request">The credentials of the user to authenticate.</param>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        Result<LoginResponse> result = await Service.LoginAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
    
    #endregion
    
    #region RefreshAsync
    
    /// <summary>
    /// Validates a refresh token, rotates it, and issues a new JSON Web Token and refresh token asynchronously.
    /// </summary>
    /// <param name="request">The refresh token presented by the client.</param>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest request)
    {
        Result<LoginResponse> result = await Service.RefreshAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
    
    #endregion
}
