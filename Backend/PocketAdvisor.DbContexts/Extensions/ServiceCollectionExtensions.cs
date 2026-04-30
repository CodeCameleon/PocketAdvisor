using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PocketAdvisor.DbContexts.Implementations;
using PocketAdvisor.DbContexts.Interfaces;

namespace PocketAdvisor.DbContexts.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region AddDataSeeder
    
    /// <summary>
    /// Adds the test data seeder to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddDataSeeder(this IServiceCollection services)
    {
        services.AddScoped<IDataSeeder, DataSeeder>();
    }
    
    #endregion
    
    #region AddPocketAdvisorDbContext
    
    /// <summary>
    /// Adds the <see cref="PocketAdvisorDbContext" /> to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="connectionString">The connection string to the database.</param>
    public static void AddPocketAdvisorDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<PocketAdvisorDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });
    }
    
    #endregion
    
    #region AddTransactionManager
    
    /// <summary>
    /// Adds the transaction manager to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddTransactionManager(this IServiceCollection services)
    {
        services.AddScoped<ITransactionManager, TransactionManager>();
    }
    
    #endregion
}
