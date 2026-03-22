namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the <see cref="IConfiguration" /> interface.
/// </summary>
public static class ConfigurationExtensions
{
    #region Constants
    
    /// <summary>
    /// The character used to separate configuration keys.
    /// </summary>
    private const char Colon = ':';
    
    /// <summary>
    /// The name of the section that contains database connection strings in the configuration.
    /// </summary>
    private const string ConnectionStrings = "ConnectionStrings";
    
    /// <summary>
    /// The name of the default database connection string in the configuration.
    /// </summary>
    private const string Default = "Default";
    
    /// <summary>
    /// The message template used for missing configuration keys.
    /// </summary>
    private const string KeyNotFoundMessageTemplate = "The required configuration key '{0}' is missing or empty.";
    
    #endregion
    
    #region GetDefaultConnectionString
    
    /// <summary>
    /// Gets the default database connection string from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The default database connection string.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the default database connection string is not found in the configuration.
    /// </exception>
    public static string GetDefaultConnectionString(this IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString(Default);
        
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw CreateInvalidOperationException(ConnectionStrings, Default);
        }
        
        return connectionString;
    }
    
    #endregion
    
    #region CreateInvalidOperationException
    
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidOperationException" /> for the missing key.
    /// </summary>
    /// <param name="keys">The names of the configuration keys that are not found.</param>
    /// <returns>The exception instance ready to be thrown.</returns>
    private static InvalidOperationException CreateInvalidOperationException(params string[] keys)
    {
        string message = string.Format(
            KeyNotFoundMessageTemplate,
            string.Join(Colon, keys)
        );
        
        return new(message);
    }
    
    #endregion
}
