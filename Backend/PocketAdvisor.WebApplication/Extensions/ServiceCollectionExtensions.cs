using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Resend;

namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the <see cref="IServiceCollection" /> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    #region AddPocketAdvisorAuthentication
    
    /// <summary>
    /// Adds JWT Bearer authentication to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="issuer">The valid issuer of the JSON Web Token.</param>
    /// <param name="audience">The valid audience of the JSON Web Token.</param>
    /// <param name="signingSecret">The secret used to validate the JSON Web Token signature.</param>
    public static void AddPocketAdvisorAuthentication(this IServiceCollection services,
        string issuer, string audience, string signingSecret)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingSecret))
            };
        });
    }
    
    #endregion
    
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
