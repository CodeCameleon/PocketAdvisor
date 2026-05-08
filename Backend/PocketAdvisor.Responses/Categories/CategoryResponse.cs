namespace PocketAdvisor.Responses.Categories;

/// <summary>
/// The response model that represents a category in the system.
/// </summary>
public sealed class CategoryResponse
{
    /// <summary>
    /// The unique identifier of the category.
    /// </summary>
    public required Guid Id { get; init; }
    
    /// <summary>
    /// The name of the category.
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// Indicates whether the category is global (available to all users) or personal.
    /// </summary>
    public required bool IsGlobal { get; init; }
}
