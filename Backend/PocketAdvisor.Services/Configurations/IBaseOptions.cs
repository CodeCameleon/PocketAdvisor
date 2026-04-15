namespace PocketAdvisor.Services.Configurations;

/// <summary>
/// Defines the base interface for all configuration options.
/// </summary>
public interface IBaseOptions
{
    /// <summary>
    /// The name of the configuration section that contains the options for this type.
    /// </summary>
    static abstract string SectionName { get; }
}
