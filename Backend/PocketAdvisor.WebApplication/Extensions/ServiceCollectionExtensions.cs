using System.Globalization;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PocketAdvisor.WebApplication.Constants;
using Resend;

namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region Constants
    
    /// <summary>
    /// The number of segments a single sliding rate limiting window is divided into.
    /// </summary>
    private const int SegmentsPerWindow = 6;
    
    /// <summary>
    /// The detail message returned when the caller has exceeded the rate limit.
    /// </summary>
    private const string TooManyRequestsMessage = "Too many requests. Please try again later.";
    
    /// <summary>
    /// The title returned when the caller has exceeded the rate limit.
    /// </summary>
    private const string TooManyRequestsTitle = "Too Many Requests";
    
    /// <summary>
    /// The link to the specification of the status code returned when the caller
    /// has exceeded the rate limit.
    /// </summary>
    private const string TooManyRequestsType = "https://tools.ietf.org/html/rfc6585#section-4";
    
    /// <summary>
    /// The rate limiting partition key used when the IP address of the caller is unknown.
    /// </summary>
    private const string UnknownPartitionKey = "Unknown";
    
    /// <summary>
    /// The JSON serializer options used to serialize the problem details response.
    /// </summary>
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    
    #endregion
    
    #region AddPocketAdvisorAuthentication
    
    /// <summary>
    /// Adds JWT Bearer authentication to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="issuer">The valid issuer of the JSON Web Token.</param>
    /// <param name="audience">The valid audience of the JSON Web Token.</param>
    /// <param name="signingSecret">The secret used to validate the JSON Web Token signature.</param>
    public static void AddPocketAdvisorAuthentication(this IServiceCollection services,
        string issuer, string audience, string signingSecret)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret))
            };
        });
    }
    
    #endregion
    
    #region AddPocketAdvisorCors
    
    /// <summary>
    /// Adds the global CORS policy to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="frontendBaseUrl">The base URL of the frontend application.</param>
    public static void AddPocketAdvisorCors(this IServiceCollection services, string frontendBaseUrl)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyNames.Global, policy =>
            {
                policy.WithOrigins(frontendBaseUrl).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
        });
    }
    
    #endregion
    
    #region AddPocketAdvisorRateLimiter
    
    /// <summary>
    /// Adds the rate limiter policies to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="authenticationPermitLimit">
    /// The number of requests allowed on the authentication endpoints within a single window.
    /// </param>
    /// <param name="refreshPermitLimit">
    /// The number of requests allowed on the token refresh endpoint within a single window.
    /// </param>
    /// <param name="windowSeconds">The length of a single rate limiting window in seconds.</param>
    public static void AddPocketAdvisorRateLimiter(this IServiceCollection services,
        int authenticationPermitLimit, int refreshPermitLimit, int windowSeconds)
    {
        services.AddRateLimiter(options =>
        {
            options.AddPolicy(
                RateLimiterPolicyNames.Authentication,
                context => CreatePartition(context, authenticationPermitLimit, windowSeconds)
            );
            
            options.AddPolicy(
                RateLimiterPolicyNames.Refresh,
                context => CreatePartition(context, refreshPermitLimit, windowSeconds)
            );
            
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = OnRejectedAsync;
        });
        
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.ClientErrorMapping[StatusCodes.Status429TooManyRequests] = new()
            {
                Link = TooManyRequestsType,
                Title = TooManyRequestsTitle
            };
        });
    }
    
    #endregion
    
    #region AddResendClient
    
    /// <summary>
    /// Adds the Resend client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">The api key for the Resend email service.</param>
    public static void AddResendClient(this IServiceCollection services, string apiKey)
    {
        services.AddHttpClient<ResendClient>();
        services.Configure<ResendClientOptions>(options =>
        {
            options.ApiToken = apiKey;
        });
        services.AddTransient<IResend, ResendClient>();
    }
    
    #endregion
    
    #region CreatePartition
    
    /// <summary>
    /// Creates the sliding window rate limiting partition of the caller for the given request.
    /// </summary>
    /// <param name="context">The HTTP context of the request being limited.</param>
    /// <param name="permitLimit">The number of requests allowed within a single window.</param>
    /// <param name="windowSeconds">The length of a single rate limiting window in seconds.</param>
    /// <returns>The rate limiting partition belonging to the caller.</returns>
    private static RateLimitPartition<string> CreatePartition(HttpContext context, int permitLimit,
        int windowSeconds)
    {
        string partitionKey = context.Connection.RemoteIpAddress?.ToString() ?? UnknownPartitionKey;
        
        return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new()
        {
            PermitLimit = permitLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            Window = TimeSpan.FromSeconds(windowSeconds),
            SegmentsPerWindow = SegmentsPerWindow,
            AutoReplenishment = true
        });
    }
    
    #endregion
    
    #region OnRejectedAsync
    
    /// <summary>
    /// Writes a structured problem details response when a request is rejected by the rate limiter.
    /// </summary>
    /// <param name="context">The context of the rejected request.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    private static async ValueTask OnRejectedAsync(OnRejectedContext context,
        CancellationToken cancellationToken)
    {
        HttpContext httpContext = context.HttpContext;
        
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString(
                CultureInfo.InvariantCulture
            );
        }
        
        ProblemDetailsFactory problemDetailsFactory = httpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>();
        
        ProblemDetails problemDetails = problemDetailsFactory.CreateProblemDetails(
            httpContext,
            StatusCodes.Status429TooManyRequests,
            detail: TooManyRequestsMessage
        );
        
        string json = JsonSerializer.Serialize(problemDetails, JsonSerializerOptions);
        
        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        
        await httpContext.Response.WriteAsync(json, cancellationToken);
    }
    
    #endregion
}
