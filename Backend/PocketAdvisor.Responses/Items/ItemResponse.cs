using PocketAdvisor.Enums;

namespace PocketAdvisor.Responses.Items;

/// <summary>
/// The response model that represents an item in the system.
/// </summary>
public sealed class ItemResponse
{
    /// <summary>
    /// The unique identifier of the item.
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The name of the item.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The unit category of the item.
    /// </summary>
    public required EUnitCategory UnitCategory { get; init; }
}
