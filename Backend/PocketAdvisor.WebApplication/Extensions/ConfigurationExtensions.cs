namespace PocketAdvisor.WebApplication.Extensions;

/// <summary>
/// The extension methods for the <see cref="IConfiguration" /> interface.
/// </summary>
public static class ConfigurationExtensions
{
    #region Constants
    
    /// <summary>
    /// The name of the api key for the Resend service in the configuration.
    /// </summary>
    private const string ApiKey = "ApiKey";
    
    /// <summary>
    /// The character used to separate configuration keys.
    /// </summary>
    private const char Colon = ':';
    
    /// <summary>
    /// The name of the section that contains database connection strings in the configuration.
    /// </summary>
    private const string ConnectionStrings = "ConnectionStrings";
    
    /// <summary>
    /// The template for the database connection string.
    /// </summary>
    private const string ConnectionStringTemplate = "Host={0};Port={1};Database={2};Username={3};Password={4}";
    
    /// <summary>
    /// The name of the default database name in the configuration.
    /// </summary>
    private const string DefaultDatabase = "DefaultDatabase";
    
    /// <summary>
    /// The name of the default database host in the configuration.
    /// </summary>
    private const string DefaultHost = "DefaultHost";
    
    /// <summary>
    /// The name of the default database password in the configuration.
    /// </summary>
    private const string DefaultPassword = "DefaultPassword";
    
    /// <summary>
    /// The name of the default database port in the configuration.
    /// </summary>
    private const string DefaultPort = "DefaultPort";
    
    /// <summary>
    /// The name of the default database username in the configuration.
    /// </summary>
    private const string DefaultUsername = "DefaultUsername";
    
    /// <summary>
    /// The name of the key file path for the secure store in the configuration.
    /// </summary>
    private const string KeyFilePath = "KeyFilePath";
    
    /// <summary>
    /// The message template used for missing configuration keys.
    /// </summary>
    private const string KeyNotFoundMessageTemplate = "The required configuration key '{0}' is missing or empty.";
    
    /// <summary>
    /// The name of the section that contains the Resend settings in the configuration.
    /// </summary>
    private const string Resend = "Resend";
    
    /// <summary>
    /// The name of the section that contains secure store settings in the configuration.
    /// </summary>
    private const string SecureStore = "SecureStore";
    
    /// <summary>
    /// The name of the secure store path in the configuration.
    /// </summary>
    private const string StorePath = "StorePath";
    
    #endregion
    
    #region GetDefaultConnectionString
    
    /// <summary>
    /// Gets the default database connection string from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The default database connection string.</returns>
    /// <exception cref="InvalidOperationException">
    /// If any required part of the connection string is missing from the configuration.
    /// </exception>
    public static string GetDefaultConnectionString(this IConfiguration configuration)
    {
        IConfigurationSection section = configuration.GetSection(ConnectionStrings);
        
        string? host = section.GetValue<string>(DefaultHost);
        if (string.IsNullOrWhiteSpace(host))
        {
            throw CreateInvalidOperationException(ConnectionStrings, DefaultHost);
        }
        
        string? port = section.GetValue<string>(DefaultPort);
        if (string.IsNullOrWhiteSpace(port))
        {
            throw CreateInvalidOperationException(ConnectionStrings, DefaultPort);
        }
        
        string? database = section.GetValue<string>(DefaultDatabase);
        if (string.IsNullOrWhiteSpace(database))
        {
            throw CreateInvalidOperationException(ConnectionStrings, DefaultDatabase);
        }
        
        string? username = section.GetValue<string>(DefaultUsername);
        if (string.IsNullOrWhiteSpace(username))
        {
            throw CreateInvalidOperationException(ConnectionStrings, DefaultUsername);
        }
        
        string? password = section.GetValue<string>(DefaultPassword);
        if (string.IsNullOrWhiteSpace(password))
        {
            throw CreateInvalidOperationException(ConnectionStrings, DefaultPassword);
        }
        
        return string.Format(ConnectionStringTemplate, host, port, database, username, password);
    }
    
    #endregion
    
    #region GetResendApiKey
    
    /// <summary>
    /// Gets the api key for the Resend service from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The api key for the Resend service.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the api key for the Resend service is not found in the configuration.
    /// </exception>
    public static string GetResendApiKey(this IConfiguration configuration)
    {
        string? apiKey = configuration.GetSection(Resend).GetValue<string>(ApiKey);
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw CreateInvalidOperationException(Resend, ApiKey);
        }
        
        return apiKey;
    }
    
    #endregion
    
    #region GetSecureStoreKeyFilePath
    
    /// <summary>
    /// Gets the path of the key file for the secure store from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The key file path for the secure store.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the key file path for the secure store is not found in the configuration.
    /// </exception>
    public static string GetSecureStoreKeyFilePath(this IConfiguration configuration)
    {
        string? keyFilePath = configuration.GetSection(SecureStore).GetValue<string>(KeyFilePath);
        
        if (string.IsNullOrWhiteSpace(keyFilePath))
        {
            throw CreateInvalidOperationException(SecureStore, KeyFilePath);
        }
        
        return keyFilePath;
    }
    
    #endregion
    
    #region GetSecureStorePath
    
    /// <summary>
    /// Gets the path of the secure store from the configuration.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The secure store file path.</returns>
    /// <exception cref="InvalidOperationException">
    /// If the secure store path is not found in the configuration.
    /// </exception>
    public static string GetSecureStorePath(this IConfiguration configuration)
    {
        string? secureStorePath = configuration.GetSection(SecureStore).GetValue<string>(StorePath);
        
        if (string.IsNullOrWhiteSpace(secureStorePath))
        {
            throw CreateInvalidOperationException(SecureStore, StorePath);
        }
        
        return secureStorePath;
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
