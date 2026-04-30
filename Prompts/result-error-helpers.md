# Result Error Helpers — Extension Method, Base Service Methods, and HandleFailure

**Date:** 2026-04-30

---

## Turn 1

### User

> I want you to create an extension method service assembly that create a conflict error and returns a Result Fail
> with is. You can find a conflict error in the DeleteTransactionItemAsync method.

### Assistant Thinking

- Read `TransactionService.cs` to locate the inline conflict error construction in `DeleteTransactionItemAsync` — an
`Error` with `ErrorMetadataKeys.Conflict = true` set in its `Metadata`, returned via `Result.Fail(error)`.
- Read `BaseService.cs`, `ValidationFailureExtensions.cs`, `ErrorMetadataKeys.cs`, `BaseController.cs`, and
`ServiceCollectionExtensions.cs` to understand the existing patterns.
- Decided to create `ResultExtensions.cs` in `PocketAdvisor.Services/Extensions/` with a static
`FailWithConflict(string message)` method mirroring the inline pattern.
- Updated `TransactionService.cs` to call `ResultExtensions.FailWithConflict(string.Empty)` and removed the
now-unused `using PocketAdvisor.Services.Constants` directive.

### Assistant Response

---

#### New: `Backend/PocketAdvisor.Services/Extensions/ResultExtensions.cs`

```csharp
using FluentResults;
using PocketAdvisor.Services.Constants;

namespace PocketAdvisor.Services.Extensions;

/// <summary>
/// The extension methods for the <see cref="Result" /> class.
/// </summary>
public static class ResultExtensions
{
    #region FailWithConflict

    /// <summary>
    /// Creates a failed <see cref="Result" /> with a conflict error.
    /// </summary>
    /// <param name="message">The error message describing the conflict.</param>
    /// <returns>A failed <see cref="Result" /> containing the conflict error.</returns>
    public static Result FailWithConflict(string message)
    {
        Error error = new(message)
        {
            Metadata =
            {
                [ErrorMetadataKeys.Conflict] = true
            }
        };

        return Result.Fail(error);
    }

    #endregion
}
```

---

#### Modified: `Backend/PocketAdvisor.Services/Implementations/TransactionService.cs`

Replaced the inline conflict error block in `DeleteTransactionItemAsync`:

```csharp
// Before
Error error = new(string.Empty)
{
    Metadata =
    {
        [ErrorMetadataKeys.Conflict] = true
    }
};
return Result.Fail(error);

// After
return ResultExtensions.FailWithConflict(string.Empty);
```

Removed `using PocketAdvisor.Services.Constants;` (no longer referenced directly).

---

### Design Decisions

- Placed `ResultExtensions` in the existing `Extensions/` folder alongside `ValidationFailureExtensions` for
consistency — both are utility helpers that operate on FluentResults types.
- Method returns a `Result` directly (not just an `Error`) to keep call sites a single expression, matching the
existing `Result.Fail(CreateError(...))` pattern.

---

## Turn 2

### User

> I have made it into a base service method instead to be easier to use. What do you think?

### Assistant Thinking

- Read the updated `BaseService.cs` — the user had added `CreateConflictError()` as a `protected static` method
returning an `Error`, placed in its own `#region`, following the same structure as the existing
`CreateError(message, propertyName)`.
- Verified `TransactionService.cs` now calls `Result.Fail(CreateConflictError())`.
- Confirmed the user had deleted `ResultExtensions.cs`.
- Assessed the approach: `CreateConflictError()` sits alongside `CreateError()`, both error-construction helpers
in the same class. Call-site symmetry with `Result.Fail(CreateConflictError())` mirrors
`Result.Fail(CreateError(...))`. All error construction responsibility is centralised in `BaseService`.

### Assistant Response

Agreed it's a better approach. Key reasons:

- **Consistency** — `CreateConflictError()` sits right alongside `CreateError(message, propertyName)`, so both
error-construction helpers live in the same place.
- **Encapsulation** — the `Error` construction detail stays inside `BaseService`, which already owns that
responsibility for property errors.
- **Call-site symmetry** — `return Result.Fail(CreateConflictError())` mirrors
`return Result.Fail(CreateError(...))` exactly.

---

## Turn 3

### User

> Now I want you to create a same kind of method for the not found error too.

### Assistant Thinking

- Grepped all services for `Result.Fail(string.Empty)` — found not-found returns in `AccountService` (×2),
`CategoryService` (×4), `ItemService` (×2), `TransactionService` (×3, excluding the conflict one).
- Read all controller files to understand how not-found results were currently detected — a fragile heuristic:
`string.IsNullOrEmpty(e.Message) && !e.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out _)`.
- Plan: add `NotFound` constant to `ErrorMetadataKeys`, add `CreateNotFoundError()` to `BaseService`, replace all
`Result.Fail(string.Empty)` calls in services, and clean up the heuristic checks in controllers to use the proper key.
- Also caught that `DeleteTransactionItemAsync` in `TransactionController` still had the old `string.IsNullOrEmpty`
guard on the `Conflict` check — fixed that too.

### Assistant Response

---

#### Modified: `Backend/PocketAdvisor.Services/Constants/ErrorMetadataKeys.cs`

```csharp
/// <summary>
/// The metadata key that marks an error as a not found.
/// </summary>
public const string NotFound = "NotFound";
```

---

#### Modified: `Backend/PocketAdvisor.Services/Implementations/BaseService.cs`

Added `CreateNotFoundError()` before `CreateConflictError()`:

```csharp
#region CreateNotFoundError

/// <summary>
/// Creates a new not found <see cref="Error" /> that can be returned in a <see cref="Result" /> object.
/// </summary>
/// <returns>The constructed not found <see cref="Error" /> ready to be returned.</returns>
protected static Error CreateNotFoundError()
{
    Error error = new(string.Empty)
    {
        Metadata =
        {
            [ErrorMetadataKeys.NotFound] = true
        }
    };

    return error;
}

#endregion
```

---

#### Modified: `AccountService.cs`, `CategoryService.cs`, `ItemService.cs`, `TransactionService.cs`

All `Result.Fail(string.Empty)` not-found returns replaced:

```csharp
// Before
return Result.Fail(string.Empty);

// After
return Result.Fail(CreateNotFoundError());
```

---

#### Modified: `AccountController.cs`, `CategoryController.cs`, `ItemController.cs`, `TransactionController.cs`

Fragile heuristic replaced with explicit metadata key check:

```csharp
// Before
if (result.Errors.Any(e => string.IsNullOrEmpty(e.Message) &&
    !e.Metadata.TryGetValue(ErrorMetadataKeys.PropertyName, out _)))
{
    return NotFound();
}

// After
if (result.Errors.Any(e => e.Metadata.TryGetValue(ErrorMetadataKeys.NotFound, out _)))
{
    return NotFound();
}
```

---

### Design Decisions

- `CreateNotFoundError()` placed before `CreateConflictError()` in `BaseService` to keep the pair together in
alphabetical order.
- The old controller heuristic (checking for empty message and absence of `PropertyName`) was removed in favour
of the explicit `NotFound` key, making the intent unambiguous and symmetric with how `Conflict` is handled.

---

## Turn 4

### User

> I want you to create a base controller method that will be called when the Result returns with a Fail. It
> should check the errors and send back the proper return value (NotFound, Conflict or BadRequest) to the client.

### Assistant Thinking

- Read `BaseController.cs` and all four controllers to catalogue every `if (result.IsFailed)` block.
- Identified three distinct failure shapes: `NotFound` metadata → 404, `Conflict` metadata → 409, anything else →
400 via existing `BadRequest(IReadOnlyList<IError>)`.
- Also spotted that `DeleteTransactionItemAsync` in `TransactionController` still carried the old
`string.IsNullOrEmpty` guard on the `Conflict` check — cleaned that up too.
- After adding `HandleFailure`, all `using PocketAdvisor.Services.Constants` directives in the concrete controllers
became unused and were removed — `ErrorMetadataKeys` is now only referenced in `BaseController`.

### Assistant Response

---

#### Modified: `Backend/PocketAdvisor.WebApplication/Controllers/BaseController.cs`

Added `HandleFailure(Result result)` before the existing `BadRequest` region:

```csharp
#region HandleFailure

/// <summary>
/// Inspects the errors of a failed <see cref="Result" /> and returns the appropriate
/// <see cref="IActionResult" />: <see cref="NotFoundResult" /> if any error is marked as not found,
/// <see cref="ConflictResult" /> if any error is marked as a conflict, or a
/// <see cref="BadRequestObjectResult" /> with validation problem details otherwise.
/// </summary>
/// <param name="result">The failed result whose errors should be inspected.</param>
/// <returns>The appropriate <see cref="IActionResult" /> for the client.</returns>
protected IActionResult HandleFailure(Result result)
{
    if (result.Errors.Any(e => e.Metadata.TryGetValue(ErrorMetadataKeys.NotFound, out _)))
    {
        return NotFound();
    }

    if (result.Errors.Any(e => e.Metadata.TryGetValue(ErrorMetadataKeys.Conflict, out _)))
    {
        return Conflict();
    }

    return BadRequest(result.Errors);
}

#endregion
```

---

#### Modified: `AccountController.cs`, `CategoryController.cs`, `ItemController.cs`, `TransactionController.cs`

Every `if (result.IsFailed)` block replaced with a single call:

```csharp
// Before (example from TransactionController.DeleteTransactionItemAsync)
if (result.IsFailed)
{
    if (result.Errors.Any(e => string.IsNullOrEmpty(e.Message) &&
        e.Metadata.TryGetValue(ErrorMetadataKeys.Conflict, out _)))
    {
        return Conflict();
    }
    return NotFound();
}

// After (all controllers)
if (result.IsFailed)
{
    return HandleFailure(result);
}
```

Removed `using PocketAdvisor.Services.Constants` from all four controllers — `ErrorMetadataKeys` is now referenced
only in `BaseController`.

---

### Design Decisions

- `NotFound` is checked before `Conflict` inside `HandleFailure` — a missing resource is a more fundamental failure
than a business-rule violation, so 404 takes priority if both keys were ever present.
- The method delegates to the existing `BadRequest(IReadOnlyList<IError>)` overload for the fallback case, so
validation problem details continue to be built the same way.
- `HandleFailure` takes a `Result` rather than `IReadOnlyList<IError>` to keep call sites clean — callers already
have the `Result` in hand and shouldn't need to dereference `.Errors` themselves.

---

## Turn 5

### User

> Export

### Assistant Thinking

- Executed the Export macro per memory instructions.
- Memory consolidation pass: all four memory files reviewed — no overlaps, staleness, or files to retire.
Index unchanged.
- Determined filename from topic: `result-error-helpers.md`.
