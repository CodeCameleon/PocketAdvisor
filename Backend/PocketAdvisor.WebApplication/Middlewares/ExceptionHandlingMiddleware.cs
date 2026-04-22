using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace PocketAdvisor.WebApplication.Middlewares;

/// <summary>
/// Represents the middleware that catches unhandled exceptions and returns a
/// structured <see cref="ProblemDetails" /> response.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    #region Constants
    
    /// <summary>
    /// The default error message returned in production when an unhandled exception occurs.
    /// </summary>
    private const string DefaultErrorMessage = "An unexpected error occurred. Please try again later.";
    
    /// <summary>
    /// The JSON serializer options used to serialize the problem details response.
    /// </summary>
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware" /> class.
    /// </summary>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <param name="logger">The logger used to record exceptions.</param>
    /// <param name="environment">The host environment used to determine the current mode.</param>
    /// <param name="problemDetailsFactory">The factory used to create problem details responses.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment, ProblemDetailsFactory problemDetailsFactory)
    {
        ArgumentNullException.ThrowIfNull(next, nameof(next));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));
        ArgumentNullException.ThrowIfNull(environment, nameof(environment));
        ArgumentNullException.ThrowIfNull(problemDetailsFactory, nameof(problemDetailsFactory));
        
        Environment = environment;
        Logger = logger;
        Next = next;
        ProblemDetailsFactory = problemDetailsFactory;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The host environment used to determine the current mode.
    /// </summary>
    private IHostEnvironment Environment { get; }
    
    /// <summary>
    /// The logger used to record exceptions.
    /// </summary>
    private ILogger<ExceptionHandlingMiddleware> Logger { get; }
    
    /// <summary>
    /// The next middleware delegate in the pipeline.
    /// </summary>
    private RequestDelegate Next { get; }
    
    /// <summary>
    /// The factory used to create problem details responses.
    /// </summary>
    private ProblemDetailsFactory ProblemDetailsFactory { get; }
    
    #endregion
    
    #region InvokeAsync
    
    /// <summary>
    /// Invokes the middleware, catching any unhandled exception thrown further down the pipeline.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await Next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }
    
    #endregion
    
    #region HandleExceptionAsync
    
    /// <summary>
    /// Handles the given exception based on the host environment.
    /// </summary>
    /// <param name="context">The HTTP context of the exception.</param>
    /// <param name="exception">The exception to handle.</param>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (Logger.IsEnabled(LogLevel.Error))
        {
            Logger.LogError(
                exception,
                "An unhandled exception occurred while processing the request. TraceId: {TraceId}",
                context.TraceIdentifier
            );
        }
        
        string detail = DefaultErrorMessage;
        if (Environment.IsDevelopment())
        {
            detail = exception.Message;
        }
        
        ProblemDetails problemDetails = ProblemDetailsFactory.CreateProblemDetails(
            context,
            detail: detail
        );
        
        string json = JsonSerializer.Serialize(problemDetails, JsonSerializerOptions);
        
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        
        await context.Response.WriteAsync(json);
    }
    
    #endregion
}
