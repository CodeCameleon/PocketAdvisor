using Microsoft.Extensions.DependencyInjection;
using PocketAdvisor.Services.Implementations;
using PocketAdvisor.Services.Interfaces;

namespace PocketAdvisor.Services.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddServices(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
    }
}
