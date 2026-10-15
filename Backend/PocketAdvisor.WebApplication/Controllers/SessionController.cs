using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Responses.Users;
using PocketAdvisor.Services.Interfaces;
using PocketAdvisor.WebApplication.Constants;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling session-related operations.
/// </summary>
[Route(RouteTemplate)]
public sealed class SessionController
    : BaseController<IUserService>
{
    #region Constants
    
    /// <summary>
    /// The route template of the controller.
    /// </summary>
    private const string RouteTemplate = "api/sessions";
    
    /// <summary>
    /// The path of the refresh token cookie, so it is only sent to the session endpoints.
    /// </summary>
    private const string RefreshTokenCookiePath = "/" + RouteTemplate;
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionController" /> class.
    /// </summary>
    /// <param name="userService">The user service instance.</param>
    public SessionController(IUserService userService) : base(userService) { }
    
    #endregion
    
    #region CreateRefreshTokenCookieOptions
    
    /// <summary>
    /// Creates the options of the refresh token cookie.
    /// </summary>
    /// <param name="expires">The expiration date and time of the cookie.</param>
    /// <returns>The created cookie options.</returns>
    private static CookieOptions CreateRefreshTokenCookieOptions(DateTimeOffset? expires = null)
    {
        return new()
        {
            Path = RefreshTokenCookiePath,
            Expires = expires,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true
        };
    }
    
    #endregion
    
    #region LoginAsync
    
    /// <summary>
    /// Authenticates a user and issues a JSON Web Token and refresh token asynchronously.
    /// </summary>
    /// <param name="request">The credentials of the user to authenticate.</param>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimiterPolicyNames.Authentication)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
    {
        Result<SessionResponse> result = await Service.LoginAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        Response.Cookies.Append(
            CookieNames.RefreshToken,
            result.Value.RefreshToken,
            CreateRefreshTokenCookieOptions(
                new DateTimeOffset(result.Value.RefreshTokenExpiryAt, TimeSpan.Zero)
            )
        );
        
        return StatusCode(StatusCodes.Status201Created, new LoginResponse
        {
            JsonWebToken = result.Value.JsonWebToken
        });
    }
    
    #endregion
    
    #region LogoutAsync
    
    /// <summary>
    /// Revokes the refresh token of the current session and clears its cookie asynchronously.
    /// </summary>
    [HttpPost("logout")]
    [EnableRateLimiting(RateLimiterPolicyNames.Refresh)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> LogoutAsync()
    {
        RefreshRequest request = new()
        {
            RefreshToken = Request.Cookies[CookieNames.RefreshToken]
        };
        
        await Service.LogoutAsync(request);
        
        Response.Cookies.Delete(CookieNames.RefreshToken, CreateRefreshTokenCookieOptions());
        
        return NoContent();
    }
    
    #endregion
    
    #region RefreshAsync
    
    /// <summary>
    /// Validates the refresh token cookie, rotates it, and issues a new JSON Web Token and
    /// refresh token asynchronously.
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimiterPolicyNames.Refresh)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RefreshAsync()
    {
        RefreshRequest request = new()
        {
            RefreshToken = Request.Cookies[CookieNames.RefreshToken]
        };
        
        Result<SessionResponse> result = await Service.RefreshAsync(request);
        
        if (result.IsFailed)
        {
            Response.Cookies.Delete(CookieNames.RefreshToken, CreateRefreshTokenCookieOptions());
            
            return BadRequest(result.Errors);
        }
        
        Response.Cookies.Append(
            CookieNames.RefreshToken,
            result.Value.RefreshToken,
            CreateRefreshTokenCookieOptions(
                new DateTimeOffset(result.Value.RefreshTokenExpiryAt, TimeSpan.Zero)
            )
        );
        
        return StatusCode(StatusCodes.Status201Created, new LoginResponse
        {
            JsonWebToken = result.Value.JsonWebToken
        });
    }
    
    #endregion
}
