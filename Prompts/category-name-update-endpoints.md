# Category Name Update Endpoints

**Date:** 2026-04-28

---

## Turn 1

### User

> I want you to create two endpoints for the categories. One is for admins to update the name of the
> categories, the other is for users to update the name of their own categories.

### Assistant Thinking

Explored the full project structure before writing any code. Key findings:

- **.NET 10 layered architecture**: Entities → Repositories → Services → Controllers, with FluentValidation,
FluentResults, and EF Core.
- **Category scoping**: `Category.UserId == null` means global (admin-owned), `UserId == <guid>` means personal
(user-owned).
- **Existing pattern to follow**: `AccountService.UpdateAccountNameAsync` — validate, fetch with `asTracking: true`,
return `Result.Fail(string.Empty)` for not-found (signals 404), check name uniqueness, open transaction, assign,
commit.
- **Not-found detection in controller**: checks
`string.IsNullOrEmpty(e.Message) && !e.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out _)` to distinguish a
404 from a 400.
- **Global create consolidation logic**: when a global category is created, any personal categories with the same
name have their transactions re-pointed to the new global category and are then deleted. This same logic must apply
when renaming a global category.
- **Route naming**: personal endpoints are prefixed with `/personal`, global with `/global` — e.g.
`POST /api/categories/personal` and `POST /api/categories/global`.
- **Resource files**: both `PocketAdvisor.Requests` and `PocketAdvisor.Services` have separate
`ValidationMessages.resx` + `Designer.cs` pairs; new messages must be added to both the `.resx` and the hand-maintained
`Designer.cs`.

Files read: `CategoryService.cs`, `CategoryController.cs`, `ICategoryService.cs`, `AccountService.cs`,
`AccountController.cs`, `UpdateAccountNameRequest.cs`, `UpdateAccountNameRequestValidator.cs`,
`ValidationMessages.resx` (both projects), `ValidationMessages.Designer.cs` (both projects), `ErrorMetadataKeys.cs`.

### Assistant Response

#### New: `PocketAdvisor.Requests/Categories/UpdateCategoryNameRequest.cs`

```csharp
namespace PocketAdvisor.Requests.Categories;

/// <summary>
/// The request model for updating the name of an existing category.
/// </summary>
public sealed class UpdateCategoryNameRequest
{
    /// <summary>
    /// The new name of the category.
    /// </summary>
    public string? Name { get; set; }
}
```

---

#### New: `PocketAdvisor.Requests/Categories/UpdateCategoryNameRequestValidator.cs`

```csharp
using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Categories;

/// <summary>
/// The validator for the <see cref="UpdateCategoryNameRequest" /> model.
/// </summary>
public sealed class UpdateCategoryNameRequestValidator
    : AbstractValidator<UpdateCategoryNameRequest>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryNameRequestValidator" /> class.
    /// </summary>
    public UpdateCategoryNameRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.CategoryNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.CategoryNameTooLong);
    }

    #endregion
}
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.resx`

Added `CategoryNotFound` entry:

```xml
<data name="CategoryNotFound" xml:space="preserve">
    <value>The category was not found</value>
</data>
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`

Added corresponding property after `CategoryNameAlreadyExists`:

```csharp
/// <summary>
///   Looks up a localized string similar to The category was not found.
/// </summary>
internal static string CategoryNotFound {
    get {
        return ResourceManager.GetString("CategoryNotFound", resourceCulture);
    }
}
```

---

#### Modified: `PocketAdvisor.Services/Interfaces/ICategoryService.cs`

Added two new method signatures:

```csharp
/// <summary>
/// Updates the name of the specified global category asynchronously.
/// </summary>
Task<Result> UpdateGlobalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request);

/// <summary>
/// Updates the name of the specified personal category belonging to the given user asynchronously.
/// </summary>
Task<Result> UpdatePersonalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request, Guid userId);
```

---

#### Modified: `PocketAdvisor.Services/Implementations/CategoryService.cs`

Added `UpdateGlobalCategoryNameAsync` and `UpdatePersonalCategoryNameAsync` methods:

```csharp
#region UpdateGlobalCategoryNameAsync

/// <inheritdoc />
public async Task<Result> UpdateGlobalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Updating name of global category '{CategoryId}'...", categoryId);
    }

    IValidator<UpdateCategoryNameRequest> validator = GetValidator<UpdateCategoryNameRequest>();
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Validation failed for UpdateCategoryNameRequest: {Errors}",
                validationResult.Errors
            );
        }

        return Result.Fail(validationResult.Errors.ToErrorList());
    }

    string normalizedName = request.Name!.Trim();

    Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
        c => c.Id == categoryId && c.UserId == null,
        asTracking: true
    );

    if (category is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning("Global category '{CategoryId}' was not found.", categoryId);
        }

        return Result.Fail(string.Empty);
    }

    bool nameExists = await CategoryRepository.ExistsAsync(
        c => c.UserId == null && c.Name == normalizedName && c.Id != categoryId
    );

    if (nameExists)
    {
        return Result.Fail(
            CreateError(ValidationMessages.CategoryNameAlreadyExists, nameof(request.Name))
        );
    }

    await TransactionManager.Value.BeginTransactionAsync();

    category.Name = normalizedName;

    await TransactionManager.Value.SaveChangesAsync();

    IReadOnlyList<Category> personalCategories = await CategoryRepository.GetAllAsync(
        c => c.UserId != null && c.Name == normalizedName
    );

    foreach (Category personalCategory in personalCategories)
    {
        IReadOnlyList<Transaction> transactions = await TransactionRepository.GetAllAsync(
            t => t.CategoryId == personalCategory.Id
        );

        foreach (Transaction transaction in transactions)
        {
            transaction.CategoryId = category.Id;
            TransactionRepository.Update(transaction);
        }

        CategoryRepository.Delete(personalCategory);
    }

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Global category '{CategoryId}' name updated successfully.", categoryId);
    }

    return Result.Ok();
}

#endregion

#region UpdatePersonalCategoryNameAsync

/// <inheritdoc />
public async Task<Result> UpdatePersonalCategoryNameAsync(Guid categoryId, UpdateCategoryNameRequest request,
    Guid userId)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Updating name of personal category '{CategoryId}'...", categoryId);
    }

    IValidator<UpdateCategoryNameRequest> validator = GetValidator<UpdateCategoryNameRequest>();
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Validation failed for UpdateCategoryNameRequest: {Errors}",
                validationResult.Errors
            );
        }

        return Result.Fail(validationResult.Errors.ToErrorList());
    }

    string normalizedName = request.Name!.Trim();

    Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
        c => c.Id == categoryId && c.UserId == userId,
        asTracking: true
    );

    if (category is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Personal category '{CategoryId}' was not found for user '{UserId}'.",
                categoryId,
                userId
            );
        }

        return Result.Fail(string.Empty);
    }

    bool nameExists = await CategoryRepository.ExistsAsync(
        c => c.Name == normalizedName && (c.UserId == null || c.UserId == userId) && c.Id != categoryId
    );

    if (nameExists)
    {
        return Result.Fail(
            CreateError(ValidationMessages.CategoryNameAlreadyExists, nameof(request.Name))
        );
    }

    await TransactionManager.Value.BeginTransactionAsync();

    category.Name = normalizedName;

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Personal category '{CategoryId}' name updated successfully.", categoryId);
    }

    return Result.Ok();
}

#endregion
```

---

#### Modified: `PocketAdvisor.WebApplication/Controllers/CategoryController.cs`

Added two new endpoints:

```csharp
#region UpdateGlobalCategoryNameAsync

/// <summary>
/// Updates the name of the specified global category asynchronously.
/// Requires the <c>Administrator</c> role.
/// </summary>
/// <param name="id">The identifier of the global category to update.</param>
/// <param name="request">The new name for the category.</param>
[HttpPatch("global/{id:guid}/name")]
[Authorize(Roles = "Administrator")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateGlobalCategoryNameAsync([FromRoute] Guid id,
    [FromBody] UpdateCategoryNameRequest request)
{
    Result result = await Service.UpdateGlobalCategoryNameAsync(id, request);

    if (result.IsFailed)
    {
        if (result.Errors.Any(e => string.IsNullOrEmpty(e.Message) &&
            !e.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out _)))
        {
            return NotFound();
        }

        return BadRequest(result.Errors);
    }

    return NoContent();
}

#endregion

#region UpdatePersonalCategoryNameAsync

/// <summary>
/// Updates the name of the specified personal category belonging to the currently authenticated user
/// asynchronously.
/// </summary>
/// <param name="id">The identifier of the personal category to update.</param>
/// <param name="request">The new name for the category.</param>
[HttpPatch("personal/{id:guid}/name")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdatePersonalCategoryNameAsync([FromRoute] Guid id,
    [FromBody] UpdateCategoryNameRequest request)
{
    Result result = await Service.UpdatePersonalCategoryNameAsync(id, request, CurrentUserId);

    if (result.IsFailed)
    {
        if (result.Errors.Any(e => string.IsNullOrEmpty(e.Message) &&
            !e.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out _)))
        {
            return NotFound();
        }

        return BadRequest(result.Errors);
    }

    return NoContent();
}

#endregion
```

### Design Decisions

- Routes follow the existing `global` / `personal` prefix convention already established by the create endpoints,
giving full symmetry: `POST /api/categories/global` → `PATCH /api/categories/global/{id}/name` and
`POST /api/categories/personal` → `PATCH /api/categories/personal/{id}/name`.
- `UpdateGlobalCategoryNameAsync` replicates the consolidation logic from `CreateGlobalCategoryAsync`: after
saving the new name, any personal categories across all users that share the new name have their transactions
re-pointed to the global category and are then deleted, all within the same transaction.
- The personal name-conflict check mirrors the create check — `c.UserId == null || c.UserId == userId` — so a
user cannot rename their personal category to a name already taken by either a global category or one of their
own personal categories.

---

## Turn 2

### User

> There is a bit of a mismatch with the path of the category controller. I mean the create personal
> and the update personal endpoint.

### Assistant Thinking

The update personal endpoint was initially routed as `PATCH /api/categories/{id}/name`, missing the `/personal`
prefix that the create endpoint uses (`POST /api/categories/personal`). A one-line fix to the `[HttpPatch]` attribute.

### Assistant Response

Fixed the route on `UpdatePersonalCategoryNameAsync` from `{id:guid}/name` to `personal/{id:guid}/name`.

#### Modified: `PocketAdvisor.WebApplication/Controllers/CategoryController.cs`

```csharp
// Before
[HttpPatch("{id:guid}/name")]

// After
[HttpPatch("personal/{id:guid}/name")]
```

---

## Turn 3

### User

> Also, you forgot about the part in the global name update service method that is also in the global
> create service method.

### Assistant Thinking

The initial implementation of `UpdateGlobalCategoryNameAsync` was missing the personal-category consolidation
block present in `CreateGlobalCategoryAsync`. When a global category is renamed to a name that already exists as
personal categories for any users, those personal categories need to have their transactions re-pointed to the
global category and then be deleted — within the same transaction. The fix inserts that loop between
`SaveChangesAsync` (to flush the rename so EF tracks the updated ID) and `CommitTransactionAsync`.

### Assistant Response

Added the consolidation block to `UpdateGlobalCategoryNameAsync` in `CategoryService.cs`.

#### Modified: `PocketAdvisor.Services/Implementations/CategoryService.cs`

```csharp
await TransactionManager.Value.BeginTransactionAsync();

category.Name = normalizedName;

await TransactionManager.Value.SaveChangesAsync();

IReadOnlyList<Category> personalCategories = await CategoryRepository.GetAllAsync(
    c => c.UserId != null && c.Name == normalizedName
);

foreach (Category personalCategory in personalCategories)
{
    IReadOnlyList<Transaction> transactions = await TransactionRepository.GetAllAsync(
        t => t.CategoryId == personalCategory.Id
    );

    foreach (Transaction transaction in transactions)
    {
        transaction.CategoryId = category.Id;
        TransactionRepository.Update(transaction);
    }

    CategoryRepository.Delete(personalCategory);
}

await TransactionManager.Value.CommitTransactionAsync();
```
