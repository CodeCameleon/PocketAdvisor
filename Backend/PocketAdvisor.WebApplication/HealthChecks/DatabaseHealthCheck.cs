using Microsoft.Extensions.Diagnostics.HealthChecks;
using PocketAdvisor.DbContexts;

namespace PocketAdvisor.WebApplication.HealthChecks;

/// <summary>
/// Represents the health check that verifies whether the database can be reached.
/// </summary>
public sealed class DatabaseHealthCheck
    : IHealthCheck
{
    #region Constants
    
    /// <summary>
    /// The message to be returned when the database cannot be reached.
    /// </summary>
    private const string UnreachableMessage = "The database cannot be reached.";
    
    #endregion
    
    #region Constructors
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseHealthCheck" /> class.
    /// </summary>
    /// <param name="context">The database context instance.</param>
    /// <exception cref="ArgumentNullException">
    /// If the given parameter is <see langword="null" />.
    /// </exception>
    public DatabaseHealthCheck(PocketAdvisorDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        
        Context = context;
    }
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// The database context instance.
    /// </summary>
    private PocketAdvisorDbContext Context { get; }
    
    #endregion
    
    #region CheckHealthAsync
    
    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        bool canConnect = await Context.Database.CanConnectAsync(cancellationToken);
        
        if (canConnect)
        {
            return HealthCheckResult.Healthy();
        }
        
        return new(context.Registration.FailureStatus, UnreachableMessage);
    }
    
    #endregion
}
