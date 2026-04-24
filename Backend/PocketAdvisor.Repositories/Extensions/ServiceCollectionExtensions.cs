using Microsoft.Extensions.DependencyInjection;
using PocketAdvisor.Repositories.Implementations;
using PocketAdvisor.Repositories.Interfaces;

namespace PocketAdvisor.Repositories.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the repositories to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ITokenRepository, TokenRepository>();
        services.AddScoped<ITransactionItemRepository, TransactionItemRepository>();
        services.AddScoped<ITransactionRepository, TransactionRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
    }
}
