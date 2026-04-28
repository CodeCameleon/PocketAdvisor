# Update Item Name Endpoint

**Date:** 2026-04-28

---

## Turn 1

### User

> I want you to create the update item name endpoint. You can use the update account name endpoint as a reference.

### Assistant Thinking

Read memory for style guidelines — infer style from existing project code, follow REST conventions. Then explored the
codebase to find all relevant reference files:

- Read `UpdateAccountNameRequest.cs`, `UpdateAccountNameRequestValidator.cs` — the request/validator pattern to mirror
- Read `IAccountService.cs` — to understand the interface signature pattern
- Read `AccountService.cs` → `UpdateAccountNameAsync` — the full service implementation pattern: validate → fetch
(404 if missing) → check name collision → begin transaction → mutate → commit
- Read `AccountController.cs` → `UpdateAccountNameAsync` — the controller pattern: `PATCH {id:guid}/name`, 204/400/404
responses, error discrimination logic using `ErrorMetadataKeys`
- Read `IItemService.cs`, `ItemService.cs`, `ItemController.cs` — the existing item layer to extend
- Read both `ValidationMessages.resx` / `ValidationMessages.Designer.cs` (Requests + Services projects) — confirmed
`ItemNameRequired`, `ItemNameTooLong`, `ItemNameAlreadyExists` resource strings already exist; no new resource
entries needed

Plan: create `UpdateItemNameRequest` + validator, add method to `IItemService`, implement in `ItemService`, add
`PATCH` endpoint to `ItemController`, add `using PocketAdvisor.Services.Constants` to controller.

### Assistant Response

Six files changed in total.

---

#### New: `PocketAdvisor.Requests/Items/UpdateItemNameRequest.cs`

```csharp
namespace PocketAdvisor.Requests.Items;

/// <summary>
/// The request model for updating the name of an existing item.
/// </summary>
public sealed class UpdateItemNameRequest
{
    /// <summary>
    /// The new name of the item.
    /// </summary>
    public string? Name { get; set; }
}
```

---

#### New: `PocketAdvisor.Requests/Items/UpdateItemNameRequestValidator.cs`

```csharp
using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Items;

/// <summary>
/// The validator for the <see cref="UpdateItemNameRequest" /> model.
/// </summary>
public sealed class UpdateItemNameRequestValidator
    : AbstractValidator<UpdateItemNameRequest>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateItemNameRequestValidator" /> class.
    /// </summary>
    public UpdateItemNameRequestValidator()
    {
        RuleFor(r => r.Name).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ItemNameRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ItemNameTooLong);
    }

    #endregion
}
```

---

#### Modified: `PocketAdvisor.Services/Interfaces/IItemService.cs`

Added method declaration after `CreateItemAsync`:

```csharp
/// <summary>
/// Updates the name of the specified item asynchronously.
/// </summary>
/// <param name="itemId">The identifier of the item to update.</param>
/// <param name="request">The new name for the item.</param>
/// <param name="userId">The identifier of the user who owns the item.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result" /> indicating the success or failure of the operation.
/// </returns>
Task<Result> UpdateItemNameAsync(Guid itemId, UpdateItemNameRequest request, Guid userId);
```

---

#### Modified: `PocketAdvisor.Services/Implementations/ItemService.cs`

Added `#region UpdateItemNameAsync` after `CreateItemAsync`:

```csharp
#region UpdateItemNameAsync

/// <inheritdoc />
public async Task<Result> UpdateItemNameAsync(Guid itemId, UpdateItemNameRequest request, Guid userId)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Updating name of item '{ItemId}'...", itemId);
    }

    IValidator<UpdateItemNameRequest> validator = GetValidator<UpdateItemNameRequest>();
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Validation failed for UpdateItemNameRequest: {Errors}",
                validationResult.Errors
            );
        }

        return Result.Fail(validationResult.Errors.ToErrorList());
    }

    string normalizedName = request.Name!.Trim();

    Item? item = await ItemRepository.GetSingleOrDefaultAsync(
        i => i.Id == itemId && i.UserId == userId,
        asTracking: true
    );

    if (item is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Item '{ItemId}' was not found for user '{UserId}'.",
                itemId,
                userId
            );
        }

        return Result.Fail(string.Empty);
    }

    bool nameExists = await ItemRepository.ExistsAsync(
        i => i.UserId == userId && i.Name == normalizedName && i.Id != itemId
    );

    if (nameExists)
    {
        return Result.Fail(
            CreateError(ValidationMessages.ItemNameAlreadyExists, nameof(request.Name))
        );
    }

    await TransactionManager.Value.BeginTransactionAsync();

    item.Name = normalizedName;

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Item '{ItemId}' name updated successfully.", itemId);
    }

    return Result.Ok();
}

#endregion
```

---

#### Modified: `PocketAdvisor.WebApplication/Controllers/ItemController.cs`

Added `using PocketAdvisor.Services.Constants;` and a new `#region UpdateItemNameAsync`:

```csharp
#region UpdateItemNameAsync

/// <summary>
/// Updates the name of the specified item belonging to the currently authenticated user asynchronously.
/// </summary>
/// <param name="id">The identifier of the item to update.</param>
/// <param name="request">The new name for the item.</param>
[HttpPatch("{id:guid}/name")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateItemNameAsync([FromRoute] Guid id,
    [FromBody] UpdateItemNameRequest request)
{
    Result result = await Service.UpdateItemNameAsync(id, request, CurrentUserId);

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

---

### Design Decisions

- No new `.resx` entries were needed — `ItemNameRequired`, `ItemNameTooLong`, and `ItemNameAlreadyExists` were already
present in both resource files, matching the account equivalents.
- The 404 discrimination logic mirrors `AccountController` exactly: an error with an empty message and no
`PropertyName` metadata key signals a not-found failure (as opposed to a validation failure, which always carries a
property name).
- The name collision check excludes the item being updated (`i.Id != itemId`) so renaming an item to its current name
is a no-op rather than an error.
