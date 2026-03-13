namespace PocketAdvisor.Enums;

/// <summary>
/// The enumeration containing the possible unit categories in the system.
/// </summary>
public enum EUnitCategory
{
    /// <summary>
    /// The category for unit types that do not fit into any other category.
    /// </summary>
    Uncategorized = 1,
    
    /// <summary>
    /// The category for length unit types.
    /// </summary>
    Length = 2,
    
    /// <summary>
    /// The category for mass unit types.
    /// </summary>
    Mass = 3,
    
    /// <summary>
    /// The category for area unit types.
    /// </summary>
    Area = 4,
    
    /// <summary>
    /// The category for volume unit types.
    /// </summary>
    Volume = 5,
    
    /// <summary>
    /// The category for time unit types.
    /// </summary>
    Time = 6,
    
    /// <summary>
    /// The category for energy unit types.
    /// </summary>
    Energy = 7,
    
    /// <summary>
    /// The category for data size unit types.
    /// </summary>
    DataSize = 8
}
