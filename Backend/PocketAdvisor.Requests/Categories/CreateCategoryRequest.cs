namespace PocketAdvisor.Requests.Categories;

/// <summary>
/// The request model for creating a new category in the system.
/// </summary>
public sealed class CreateCategoryRequest
{
    /// <summary>
    /// The name of the category.
    /// </summary>
    public string? Name { get; set; }
}
