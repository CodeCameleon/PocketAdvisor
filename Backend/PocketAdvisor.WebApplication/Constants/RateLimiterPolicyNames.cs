namespace PocketAdvisor.WebApplication.Constants;

/// <summary>
/// Represents the rate limiter policy names used in the application.
/// </summary>
public static class RateLimiterPolicyNames
{
    /// <summary>
    /// The name of the rate limiter policy applied to the authentication endpoints.
    /// </summary>
    public const string Authentication = "PocketAdvisorAuthenticationRateLimiterPolicy";
    
    /// <summary>
    /// The name of the rate limiter policy applied to the token refresh endpoint.
    /// </summary>
    public const string Refresh = "PocketAdvisorRefreshRateLimiterPolicy";
}
