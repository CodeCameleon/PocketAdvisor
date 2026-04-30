namespace PocketAdvisor.Services.Constants;

/// <summary>
/// Represents the metadata keys used for error handling in the services.
/// </summary>
public static class ErrorMetadataKeys
{
    /// <summary>
    /// The metadata key that marks an error as a conflict.
    /// </summary>
    public const string Conflict = "Conflict";
    
    /// <summary>
    /// The metadata key that marks an error as a not found.
    /// </summary>
    public const string NotFound = "NotFound";
    
    /// <summary>
    /// The metadata key that stores the name of the property.
    /// </summary>
    public const string PropertyName = "PropertyName";
}
