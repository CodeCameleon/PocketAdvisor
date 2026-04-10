using NeoSmart.SecureStore;
using PocketAdvisor.Services.Configurations;

namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the <see cref="WebApplicationBuilder" /> class.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds the secrets from the secure store to the configuration.
    /// </summary>
    /// <param name="builder">The web application builder instance.</param>
    public static void AddPocketAdvisorSecrets(this WebApplicationBuilder builder)
    {
        using SecretsManager secretsManager = SecretsManager.LoadStore(
            builder.Configuration.GetSecureStorePath()
        );
        
        secretsManager.LoadKeyFromFile(
            builder.Configuration.GetSecureStoreKeyFilePath()
        );
        
        IReadOnlyDictionary<string, string?> secrets = secretsManager.Keys.Select(k =>
            new KeyValuePair<string, string?>(k, secretsManager.Get(k))
        ).ToDictionary();
        
        builder.Configuration.AddInMemoryCollection(secrets);
        
        builder.Services.AddOptions<TokenSecretsOptions>()
            .Bind(builder.Configuration.GetSection(TokenSecretsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
