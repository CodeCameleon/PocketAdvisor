using Microsoft.Extensions.Diagnostics.HealthChecks;
using PocketAdvisor.WebApplication.HealthChecks;

namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the health checks of the application.
/// </summary>
public static class HealthCheckExtensions
{
    #region Constants
    
    /// <summary>
    /// The name of the database health check.
    /// </summary>
    private const string DatabaseCheckName = "Database";
    
    /// <summary>
    /// The path of the health check endpoint.
    /// </summary>
    private const string HealthPath = "/health";
    
    /// <summary>
    /// The maximum time the database health check may take before it is reported as unhealthy.
    /// </summary>
    private static readonly TimeSpan DatabaseCheckTimeout = TimeSpan.FromSeconds(3);
    
    #endregion
    
    #region AddPocketAdvisorHealthChecks
    
    /// <summary>
    /// Adds the health checks of the application to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddPocketAdvisorHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>(
            name: DatabaseCheckName,
            failureStatus: HealthStatus.Unhealthy,
            timeout: DatabaseCheckTimeout
        );
    }
    
    #endregion
    
    #region MapPocketAdvisorHealthChecks
    
    /// <summary>
    /// Adds the health check endpoint of the application to the web application instance.
    /// </summary>
    /// <param name="application">The web application instance.</param>
    public static void MapPocketAdvisorHealthChecks(this Microsoft.AspNetCore.Builder.WebApplication application)
    {
        application.MapHealthChecks(HealthPath).AllowAnonymous();
    }
    
    #endregion
}
