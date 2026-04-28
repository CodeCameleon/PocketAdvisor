# Category Delete Endpoints

**Date:** 2026-04-28

---

## Turn 1

### User

> I want you to implement the delete endpoints for the categories. One for the global category deletion and one for
> the personal category deletion.

### Assistant Thinking

Dispatched an Explore agent to survey the codebase before writing anything. Key findings:

- **Category entity:** `UserId == null` = global category; `UserId = <guid>` = personal category.
- **Existing endpoints:** POST global, POST personal, GET all, PATCH global name, PATCH personal name — no DELETE
endpoints yet.
- **Delete pattern** (from `AccountService`, `ItemService`): fetch entity with ownership filter →
`Result.Fail(string.Empty)` if not found → `BeginTransactionAsync` → `Repository.Delete(entity)` →
`CommitTransactionAsync`.
- **Critical constraint:** `PocketAdvisorDbContext` configures `Transaction → Category` as `DeleteBehavior.Restrict`.
Deleting a category that still has linked transactions would throw at the database level — must be handled explicitly.
- **Error signalling convention:**
  - `Result.Fail(string.Empty)` → controller interprets as 404 (empty message, no `PropertyName` metadata).
  - `Result.Fail(message)` → controller interprets as 400 Bad Request.
  - `CreateError(message, propertyName)` → 400 with field-level metadata (not needed here).
- **Resource strings:** `ValidationMessages.resx` + auto-generated `ValidationMessages.Designer.cs` hold all
user-facing error messages. `CategoryNotFound` already existed; `CategoryHasTransactions` needed to be added.
- **`IBaseRepository<T>`** exposes `ExistsAsync`, `GetSingleOrDefaultAsync`, `Delete`, etc. — all available on
`ICategoryRepository` and `ITransactionRepository`.
- **Controller auth pattern:** `[Authorize(Roles = "Administrator")]` for global operations; plain `[Authorize]`
(inherited from controller) for personal.

Decision: block deletion (return 400) if the category has any linked transactions, rather than cascading deletes
or nullifying foreign keys — this is the safest approach given the `Restrict` constraint and keeps transaction
history intact.

### Assistant Response

Four files were modified.

---

#### Modified: `PocketAdvisor.Services/Interfaces/ICategoryService.cs`

Added two new method signatures at the end of the interface:

```csharp
/// <summary>
/// Deletes the specified global category asynchronously.
/// </summary>
/// <param name="categoryId">The identifier of the global category to delete.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result" /> indicating the success or failure of the operation.
/// </returns>
Task<Result> DeleteGlobalCategoryAsync(Guid categoryId);

/// <summary>
/// Deletes the specified personal category belonging to the given user asynchronously.
/// </summary>
/// <param name="categoryId">The identifier of the personal category to delete.</param>
/// <param name="userId">The identifier of the user who owns the category.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result" /> indicating the success or failure of the operation.
/// </returns>
Task<Result> DeletePersonalCategoryAsync(Guid categoryId, Guid userId);
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.resx`

Added a new resource entry:

```xml
<data name="CategoryHasTransactions" xml:space="preserve">
    <value>The category cannot be deleted because it still has transactions associated with it</value>
</data>
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`

Added the corresponding strongly-typed property (manually, since the file is auto-generated but .NET isn't
available in the sandbox):

```csharp
/// <summary>
///   Looks up a localized string similar to The category cannot be deleted because it still has transactions associated with it.
/// </summary>
internal static string CategoryHasTransactions {
    get {
        return ResourceManager.GetString("CategoryHasTransactions", resourceCulture);
    }
}
```

---

#### Modified: `PocketAdvisor.Services/Implementations/CategoryService.cs`

Added two new `#region` blocks between `UpdateGlobalCategoryNameAsync` and `UpdatePersonalCategoryNameAsync`:

```csharp
#region DeleteGlobalCategoryAsync

/// <inheritdoc />
public async Task<Result> DeleteGlobalCategoryAsync(Guid categoryId)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Deleting global category '{CategoryId}'...", categoryId);
    }

    Category? globalCategory = await CategoryRepository.GetSingleOrDefaultAsync(
        c => c.Id == categoryId && c.UserId == null
    );

    if (globalCategory is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning("Global category '{CategoryId}' was not found.", categoryId);
        }

        return Result.Fail(string.Empty);
    }

    bool hasTransactions = await TransactionRepository.ExistsAsync(
        t => t.CategoryId == categoryId
    );

    if (hasTransactions)
    {
        return Result.Fail(ValidationMessages.CategoryHasTransactions);
    }

    await TransactionManager.Value.BeginTransactionAsync();

    CategoryRepository.Delete(globalCategory);

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Global category '{CategoryId}' deleted successfully.", categoryId);
    }

    return Result.Ok();
}

#endregion

#region DeletePersonalCategoryAsync

/// <inheritdoc />
public async Task<Result> DeletePersonalCategoryAsync(Guid categoryId, Guid userId)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Deleting personal category '{CategoryId}'...", categoryId);
    }

    Category? category = await CategoryRepository.GetSingleOrDefaultAsync(
        c => c.Id == categoryId && c.UserId == userId
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

    bool hasTransactions = await TransactionRepository.ExistsAsync(
        t => t.CategoryId == categoryId
    );

    if (hasTransactions)
    {
        return Result.Fail(ValidationMessages.CategoryHasTransactions);
    }

    await TransactionManager.Value.BeginTransactionAsync();

    CategoryRepository.Delete(category);

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Personal category '{CategoryId}' deleted successfully.", categoryId);
    }

    return Result.Ok();
}

#endregion
```

---

#### Modified: `PocketAdvisor.WebApplication/Controllers/CategoryController.cs`

Added two new `#region` blocks after `UpdatePersonalCategoryNameAsync`:

```csharp
#region DeleteGlobalCategoryAsync

/// <summary>
/// Deletes the specified global category asynchronously.
/// Requires the <c>Administrator</c> role.
/// </summary>
/// <param name="id">The identifier of the global category to delete.</param>
[HttpDelete("global/{id:guid}")]
[Authorize(Roles = "Administrator")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteGlobalCategoryAsync([FromRoute] Guid id)
{
    Result result = await Service.DeleteGlobalCategoryAsync(id);

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

#region DeletePersonalCategoryAsync

/// <summary>
/// Deletes the specified personal category belonging to the currently authenticated user asynchronously.
/// </summary>
/// <param name="id">The identifier of the personal category to delete.</param>
[HttpDelete("personal/{id:guid}")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(void), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeletePersonalCategoryAsync([FromRoute] Guid id)
{
    Result result = await Service.DeletePersonalCategoryAsync(id, CurrentUserId);

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

- **Block deletion when transactions exist (400) rather than cascade-deleting or nullifying.** The DB schema
uses `DeleteBehavior.Restrict` on `Transaction → Category`, so a raw delete would throw. More importantly,
silently removing transactions or leaving them with a null category would corrupt financial history. A clear
400 error puts the decision in the caller's hands.
- **`Result.Fail(ValidationMessages.CategoryHasTransactions)` (plain string, no `PropertyName` metadata) for the
transaction-block error.** The existing controller error-dispatch logic routes empty-message errors to 404 and
non-empty errors without `PropertyName` to 400 — which is exactly the right status for a business rule violation
that isn't tied to a specific request field.
- **`CategoryHasTransactions` added to `ValidationMessages.resx` and its Designer counterpart manually.** The
Designer file is auto-generated but .NET tooling wasn't available; the hand-written property follows the identical
pattern of every other property in that file.
- **No `asTracking: true` on the fetch for delete.** Unlike the update methods (which mutate the entity in-place
and rely on change tracking), delete uses the repository's `Delete()` method which marks the entity for removal
regardless of tracking state — consistent with how account and item deletions are implemented.
