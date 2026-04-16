using Resend;

namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
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
}
