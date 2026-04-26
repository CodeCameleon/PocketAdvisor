using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PocketAdvisor.Services.Configurations;
using PocketAdvisor.Services.Implementations;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region AddServices
    
    /// <summary>
    /// Adds the services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    public static void AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FrontendOptions>(configuration);
        services.AddOptions<JsonWebTokenOptions>(configuration);
        services.AddOptions<TokenExpirationsOptions>(configuration);
        services.AddOptions<TokenSecretsOptions>(configuration);
        
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddScoped<IUserService, UserService>();
    }
    
    #endregion
    
    #region AddOptions
    
    /// <summary>
    /// Adds the options of the given type to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration instance.</param>
    /// <typeparam name="T">The type of the configuration options.</typeparam>
    private static void AddOptions<T>(this IServiceCollection services, IConfiguration configuration)
        where T : class, IBaseOptions
    {
        services.AddOptions<T>()
            .Bind(configuration.GetSection(T.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
    
    #endregion
}
