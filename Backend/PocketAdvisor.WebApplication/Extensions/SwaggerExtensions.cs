namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the Swagger documentation.
/// </summary>
public static class SwaggerExtensions
{
    #region Constants
    
    /// <summary>
    /// The name of the Swagger documentation.
    /// </summary>
    private const string SwaggerName = "PocketAdvisor API v1";
    
    /// <summary>
    /// The title of the Swagger documentation.
    /// </summary>
    private const string SwaggerTitle = "PocketAdvisor API";
    
    /// <summary>
    /// The URL of the Swagger JSON file.
    /// </summary>
    private const string SwaggerUrl = "v1/swagger.json";
    
    /// <summary>
    /// The version of the Swagger documentation.
    /// </summary>
    private const string SwaggerVersion = "v1";
    
    #endregion
    
    #region AddPocketAdvisorSwagger
    
    /// <summary>
    /// Adds the services needed for Swagger to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    public static void AddPocketAdvisorSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                SwaggerVersion,
                new()
                {
                    Title = SwaggerTitle,
                    Version = SwaggerVersion
                }
            );
        });
    }
    
    #endregion
    
    #region UsePocketAdvisorSwagger
    
    /// <summary>
    /// Adds the middleware for Swagger JSON and user interface generation.
    /// The Swagger UI is only available in the development environment.
    /// </summary>
    /// <param name="application">The web application instance.</param>
    public static void UsePocketAdvisorSwagger(this Microsoft.AspNetCore.Builder.WebApplication application)
    {
        application.UseSwagger();
        
        if (application.Environment.IsDevelopment())
        {
            application.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(SwaggerUrl, SwaggerName);
            });
        }
    }
    
    #endregion
}
