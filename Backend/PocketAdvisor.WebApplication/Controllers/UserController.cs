using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.WebApplication.Constants;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling user-related operations.
/// </summary>
[Route("api/users")]
[EnableRateLimiting(RateLimiterPolicyNames.Authentication)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public sealed class UserController
    : BaseController<IUserService>
{
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserController" /> class.
    /// </summary>
    /// <param name="userService">The user service instance.</param>
    public UserController(IUserService userService) : base(userService) { }
    
    #endregion
    
    #region CreateUserAsync
    
    /// <summary>
    /// Creates a new user in the system asynchronously.
    /// </summary>
    /// <param name="request">The data of the user to create.</param>
    [HttpPost]
    [ProducesResponseType(typeof(void), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest request)
    {
        Result result = await Service.CreateUserAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return StatusCode(StatusCodes.Status201Created);
    }
    
    #endregion
    
    #region ForgotPasswordAsync
    
    /// <summary>
    /// Sends a password reset email to the user with the given email address asynchronously.
    /// </summary>
    /// <param name="request">The email address of the user requesting a password reset.</param>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request)
    {
        Result result = await Service.ForgotPasswordAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region ResetPasswordAsync
    
    /// <summary>
    /// Resets the password of a user using the supplied password reset token asynchronously.
    /// </summary>
    /// <param name="request">The password reset token and the new password presented by the client.</param>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request)
    {
        Result result = await Service.ResetPasswordAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return NoContent();
    }
    
    #endregion
    
    #region VerifyEmailAsync
    
    /// <summary>
    /// Verifies the email address of a user using the supplied verification token asynchronously.
    /// </summary>
    /// <param name="request">The email verification token presented by the client.</param>
    [HttpPost("verify-email")]
    [ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmailAsync([FromBody] VerifyEmailRequest request)
    {
        Result result = await Service.VerifyEmailAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        return NoContent();
    }
    
    #endregion
}
