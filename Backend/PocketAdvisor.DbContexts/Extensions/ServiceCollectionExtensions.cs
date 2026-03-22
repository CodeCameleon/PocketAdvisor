using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PocketAdvisor.DbContexts.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="PocketAdvisorDbContext" /> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The connection string to the database.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPocketAdvisorDbContext(this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<PocketAdvisorDbContext>(options =>
        {
            options.UseSqlite(connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
        
        return services;
    }
}
