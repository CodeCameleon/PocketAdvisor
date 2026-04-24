using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PocketAdvisor.Requests.Users;
using PocketAdvisor.Services.Configurations;
using PocketAdvisor.Services.Interfaces;
using Resend;

namespace PocketAdvisor.WebApplication.Controllers;

/// <summary>
/// The controller responsible for handling user-related operations.
/// </summary>
[Route("api/users")]
public sealed class UserController
    : BaseController<IUserService>
{
    #region Constants
    
    /// <summary>
    /// The name of the variable used to store the time value in the email templates.
    /// </summary>
    private const string Hours = "Hours";
    
    /// <summary>
    /// The name of the variable used to store the URL value in the email templates.
    /// </summary>
    private new const string Url = "Url";
    
    /// <summary>
    /// The unique identifier of the email template used for email verification.
    /// </summary>
    private static readonly Guid EmailVerificationTemplateId = Guid.Parse("399c5102-326d-4300-88c5-ca6cc194577b");
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="UserController" /> class.
    /// </summary>
    /// <param name="userService">The user service instance.</param>
    /// <param name="tokenExpirationsOptions">
    /// The token expirations options for accessing the token expirations configuration values.
    /// </param>
    /// <param name="resend">The Resend client for sending out emails.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public UserController(IUserService userService, IOptions<TokenExpirationsOptions> tokenExpirationsOptions,
        IResend resend)
        : base(userService)
    {
        ArgumentNullException.ThrowIfNull(tokenExpirationsOptions);
        ArgumentNullException.ThrowIfNull(resend);
        
        TokenExpirationsOptions = tokenExpirationsOptions;
        Resend = resend;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The token expirations options for accessing the token expirations configuration values.
    /// </summary>
    private IOptions<TokenExpirationsOptions> TokenExpirationsOptions { get; }
    
    /// <summary>
    /// The Resend client for sending out emails.
    /// </summary>
    private IResend Resend { get; }
    
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
        Result<string> result = await Service.CreateUserAsync(request);
        
        if (result.IsFailed)
        {
            return BadRequest(result.Errors);
        }
        
        EmailMessage emailMessage = new()
        {
            From = string.Empty, // This is defined in the template.
            To = request.Email!,
            Subject = string.Empty, // This is defined in the template too.
            Template = new()
            {
                TemplateId = EmailVerificationTemplateId,
                Variables = new()
                {
                    { Hours, TokenExpirationsOptions.Value.EmailVerificationHours },
                    { Url, "" } // TODO: Add the email verification URL here.
                }
            }
        };
        await Resend.EmailSendAsync(emailMessage);
        
        return StatusCode(StatusCodes.Status201Created);
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
