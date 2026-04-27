using PocketAdvisor.Enums;

namespace PocketAdvisor.Requests.Items;

/// <summary>
/// The request model for creating a new item in the system.
/// </summary>
public sealed class CreateItemRequest
{
    /// <summary>
    /// The name of the item.
    /// </summary>
    public string? Name { get; set; }
    
    /// <summary>
    /// The unit category of the item.
    /// </summary>
    public EUnitCategory? UnitCategory { get; set; }
}
