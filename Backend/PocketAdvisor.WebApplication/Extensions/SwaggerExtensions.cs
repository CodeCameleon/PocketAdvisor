using Microsoft.OpenApi;

namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the Swagger documentation.
/// </summary>
public static class SwaggerExtensions
{
    #region Constants
    
    /// <summary>
    /// The format of the JWT Bearer token used in the Swagger documentation.
    /// </summary>
    private const string JwtBearerFormat = "JWT";
    
    /// <summary>
    /// The name of the JWT Bearer security scheme used in the Swagger documentation.
    /// </summary>
    private const string JwtBearerScheme = "Bearer";
    
    /// <summary>
    /// The description of the security scheme used in the Swagger documentation.
    /// </summary>
    private const string SchemeDescription = "Please enter a JWT Bearer token.";
    
    /// <summary>
    /// The name of the security scheme used in the Swagger documentation.
    /// </summary>
    private const string SchemeName = "Authorization";
    
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
    
    /// <summary>
    /// The template used for the generated XML documentation files.
    /// </summary>
    private const string XmlFileTemplate = "PocketAdvisor.{0}.xml";
    
    /// <summary>
    /// The list of XML documentation files to include in the Swagger documentation.
    /// </summary>
    private static readonly List<string> XmlDocumentationFiles = InitXmlDocumentationFiles(
        "Requests",
        "Responses",
        "WebApplication"
    );
    
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
            
            XmlDocumentationFiles.ForEach(file => options.IncludeXmlComments(file));
            
            OpenApiSecurityScheme securityScheme = new()
            {
                Name = SchemeName,
                Description = SchemeDescription,
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = JwtBearerScheme,
                BearerFormat = JwtBearerFormat
            };
            options.AddSecurityDefinition(JwtBearerScheme, securityScheme);
            
            options.AddSecurityRequirement(document => new()
            {
                [new(JwtBearerScheme, document)] = []
            });
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
    
    #region InitXmlDocumentationFiles
    
    /// <summary>
    /// Initializes the list of XML documentation files to include in the Swagger documentation.
    /// </summary>
    /// <param name="assemblies">The name of the assemblies to get the documentation from.</param>
    /// <returns>The list of XML documentation files.</returns>
    private static List<string> InitXmlDocumentationFiles(params string[] assemblies)
    {
        List<string> xmlFiles = [];
        
        foreach (string file in assemblies)
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                string.Format(XmlFileTemplate, file)
            );
            
            if (File.Exists(path))
            {
                xmlFiles.Add(path);
            }
        }
        
        return xmlFiles;
    }
    
    #endregion
}
