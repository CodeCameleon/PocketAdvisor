using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PocketAdvisor.Services.Clients.Interfaces;
using PocketAdvisor.Services.Configurations;
using Resend;

namespace PocketAdvisor.Services.Clients.Implementations;

/// <summary>
/// Represents the client implementation for sending out transactional emails through Resend.
/// </summary>
public sealed class ResendEmailClient
    : IEmailClient
{
    #region Constants
    
    /// <summary>
    /// The name of the variable used to store the hour value in the email templates.
    /// </summary>
    private const string Hours = "Hours";
    
    /// <summary>
    /// The name of the variable used to store the minute value in the email templates.
    /// </summary>
    private const string Minutes = "Minutes";
    
    /// <summary>
    /// The name of the variable used to store the URL value in the email templates.
    /// </summary>
    private const string Url = "Url";
    
    /// <summary>
    /// The template used to build the URL of emails.
    /// </summary>
    private const string UrlTemplate = "{0}{1}?token={2}";
    
    /// <summary>
    /// The unique identifier of the email template used for email verification.
    /// </summary>
    private const string EmailVerificationTemplateId = "399c5102-326d-4300-88c5-ca6cc194577b";
    
    /// <summary>
    /// The unique identifier of the email template used for password reset.
    /// </summary>
    private const string PasswordResetTemplateId = "4f196197-f7e1-4724-bdf4-7540c27bdaab";
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ResendEmailClient" /> class.
    /// </summary>
    /// <param name="logger">The logger for the class.</param>
    /// <param name="frontendOptions">
    /// The frontend options for accessing the frontend configuration values.
    /// </param>
    /// <param name="tokenExpirationsOptions">
    /// The token expirations options for accessing the token expirations configuration values.
    /// </param>
    /// <param name="resend">The Resend client for sending out emails.</param>
    /// <exception cref="ArgumentNullException">
    /// If any of the given parameters is <see langword="null" />.
    /// </exception>
    public ResendEmailClient(ILogger<ResendEmailClient> logger, IOptions<FrontendOptions> frontendOptions,
        IOptions<TokenExpirationsOptions> tokenExpirationsOptions, IResend resend)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(frontendOptions);
        ArgumentNullException.ThrowIfNull(tokenExpirationsOptions);
        ArgumentNullException.ThrowIfNull(resend);
        
        Logger = logger;
        FrontendOptions = frontendOptions;
        TokenExpirationsOptions = tokenExpirationsOptions;
        Resend = resend;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The logger for the class.
    /// </summary>
    private ILogger<ResendEmailClient> Logger { get; }
    
    /// <summary>
    /// The frontend options for accessing the frontend configuration values.
    /// </summary>
    private IOptions<FrontendOptions> FrontendOptions { get; }
    
    /// <summary>
    /// The token expirations options for accessing the token expirations configuration values.
    /// </summary>
    private IOptions<TokenExpirationsOptions> TokenExpirationsOptions { get; }
    
    /// <summary>
    /// The Resend client for sending out emails.
    /// </summary>
    private IResend Resend { get; }
    
    #endregion
    
    #region SendEmailVerificationAsync
    
    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        
        Logger.LogInformation("Sending email verification email...");
        
        await SendTemplateEmailAsync(
            email,
            EmailVerificationTemplateId,
            new()
            {
                {
                    Hours, TokenExpirationsOptions.Value.EmailVerificationHours
                },
                {
                    Url, BuildUrl(FrontendOptions.Value.EmailVerificationPath, token)
                }
            }
        );
        
        Logger.LogInformation("Email verification email sent successfully.");
    }
    
    #endregion
    
    #region SendPasswordResetAsync
    
    /// <inheritdoc />
    public async Task SendPasswordResetAsync(string email, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        
        Logger.LogInformation("Sending password reset email...");
        
        await SendTemplateEmailAsync(
            email,
            PasswordResetTemplateId,
            new()
            {
                {
                    Minutes, TokenExpirationsOptions.Value.PasswordResetMinutes
                },
                {
                    Url, BuildUrl(FrontendOptions.Value.PasswordResetPath, token)
                }
            }
        );
        
        Logger.LogInformation("Password reset email sent successfully.");
    }
    
    #endregion
    
    #region BuildUrl
    
    /// <summary>
    /// Builds the absolute frontend URL for the given path with the token as a query parameter.
    /// </summary>
    /// <param name="path">The path appended to the base URL of the frontend.</param>
    /// <param name="token">The plain-text token to append as a query parameter.</param>
    /// <returns>The absolute URL that needs to be embedded in the email.</returns>
    private string BuildUrl(string path, string token)
    {
        return string.Format(
            UrlTemplate,
            FrontendOptions.Value.BaseUrl,
            path,
            Uri.EscapeDataString(token)
        );
    }
    
    #endregion
    
    #region SendTemplateEmailAsync
    
    /// <summary>
    /// Sends an email based on a Resend template to the given email address asynchronously.
    /// </summary>
    /// <param name="email">The email address to send the email to.</param>
    /// <param name="templateId">The unique identifier of the Resend template.</param>
    /// <param name="variables">The variables to substitute in the template.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    private async Task SendTemplateEmailAsync(string email, string templateId,
        Dictionary<string, object> variables)
    {
        EmailMessage emailMessage = new()
        {
            From = string.Empty, // This is defined in the template.
            To = email,
            Subject = string.Empty, // This is defined in the template too.
            Template = new()
            {
                TemplateId = templateId,
                Variables = variables
            }
        };
        
        await Resend.EmailSendAsync(emailMessage);
    }
    
    #endregion
}
