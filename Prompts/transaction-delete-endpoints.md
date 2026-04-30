# Transaction Delete Endpoints

**Date:** 2026-04-30

---

## Turn 1

### User

> Let's move onto the delete endpoint for the transaction. Do you think we should create two separate
> endpoints for deletion? One for the whole transaction and items and one just for a single item
> (should not work if it is the last item)?

### Assistant Thinking

Read all transaction-related files to understand the full picture before weighing in:
- `Transaction.cs` — no direct `UserId`; ownership is derived via `FromAccount.UserId` / `ToAccount.UserId`
- `TransactionItem.cs` — composite PK `(TransactionId, ItemId)`
- `TransactionController.cs` — existing POST + GET only
- `TransactionService.cs` / `ITransactionService.cs` — existing Create + Get methods
- `PocketAdvisorDbContext.cs` — `TransactionItem → Transaction` uses `DeleteBehavior.Cascade`, so deleting a
transaction cascades to its items automatically
- `AccountController.cs` / `AccountService.cs` — reference pattern for delete endpoints (404 on not-found,
204 on success)
- `CategoryController.cs` / `CategoryService.cs` — reference pattern for conflict-style errors (returned as
BadRequest currently)
- `BaseController.cs` — `BadRequest(IReadOnlyList<IError>)` helper; no existing `Conflict` helper needed since
`ControllerBase.Conflict()` is inherited
- `ErrorMetadataKeys.cs` — only `PropertyName` exists; need a `Conflict` key
- `IBaseRepository.cs` — has `ExistsAsync`, `GetSingleOrDefaultAsync`, `GetAllAsync`, `Delete`; no `CountAsync`

Decision: two separate endpoints is clearly correct — they address different resource levels with different
preconditions.

### Assistant Response

Yes — two endpoints is the right call. The two operations represent distinct intents at distinct resource levels,
and REST guides you to them naturally:

- `DELETE /api/transactions/{id}` — deletes the whole transaction and cascades to all its items
- `DELETE /api/transactions/{transactionId}/items/{itemId}` — deletes a single item from a transaction, refuses if
it's the last one

The reason the "last item" rule makes the split feel right: a transaction with zero items is semantically invalid.
If you used a single endpoint and tried to express both operations via a query param or request body, you'd be
conflating two different operations with different preconditions into one messy endpoint. Keeping them separate
means each endpoint has a single, clear contract.

A few implementation notes worth thinking about now:

For `DELETE /api/transactions/{id}`, ownership verification should go through the transaction itself — check that
it belongs to the current user via its `FromAccountId`/`ToAccountId`. The cascade to `TransactionItem` can be handled
by EF Core's cascade delete (already configured as `DeleteBehavior.Cascade`), or you delete the items explicitly
before the transaction.

For `DELETE /api/transactions/{transactionId}/items/{itemId}`, you'll need to: verify ownership of the transaction,
check if a remaining item exists, return `409 Conflict` if it's the last one, and then delete. A `404` if the
`TransactionItem` composite key `(transactionId, itemId)` doesn't exist.

The `409 Conflict` vs `422` question: lean `409 Conflict` since the *state* of the resource (one item left) is what
prevents the operation — it's not a validation problem with the request itself.

### Design Decisions

- Two separate endpoints rather than one with a flag — each has a distinct resource target and distinct preconditions.
- `409 Conflict` over `422 Unprocessable Entity` for the last-item block — the state of the resource prevents the
operation, not malformed input.
- Cascade delete via EF Core handles `TransactionItem` cleanup on full transaction delete, no explicit loop needed.

---

## Turn 2

### User

> Yes, and go with the conflict option

### Assistant Thinking

Read all remaining files before writing any code:
- `TransactionRepository.cs` / `ITransactionRepository.cs` — thin wrappers over `BaseRepository`, no custom methods
- `TransactionItemRepository.cs` / `ITransactionItemRepository.cs` — same, no custom methods
- `BaseRepository.cs` — `ExistsAsync`, `GetSingleOrDefaultAsync`, `GetAllAsync`, `Delete` available; no `CountAsync`
- `BaseService.cs` — `CreateError` helper builds an `Error` with `PropertyName` metadata; same pattern usable
for `Conflict`
- `CategoryService.cs` + `CategoryController.cs` — `CategoryHasTransactions` is currently returned as
`BadRequest` (not `Conflict`); this is a gap in the existing codebase, not a pattern to follow for the new case
- `ValidationMessages.resx` + `Designer.cs` — need a new `TransactionLastItem` string
- `ErrorMetadataKeys.cs` — need a new `Conflict` constant to let the controller distinguish conflict errors

Plan:
1. Add `TransactionLastItem` to `.resx` + `Designer.cs`
2. Add `Conflict` constant to `ErrorMetadataKeys`
3. Add `DeleteTransactionAsync` + `DeleteTransactionItemAsync` to `ITransactionService`
4. Implement both in `TransactionService` (add `using PocketAdvisor.Services.Constants`)
5. Add both endpoints to `TransactionController`

Ownership check for both service methods: `t.FromAccountId.HasValue && t.FromAccount!.UserId == userId ||
t.ToAccountId.HasValue && t.ToAccount!.UserId == userId` — same predicate used in `GetTransactionsAsync`.

Last-item check uses `ExistsAsync(ti => ti.TransactionId == transactionId && ti.ItemId != itemId)` — avoids fetching
all items, just checks if any other item exists.

### Assistant Response

All five files modified:

---

#### Modified: `Backend/PocketAdvisor.Services/Resources/ValidationMessages.resx`

```xml
<data name="TransactionLastItem" xml:space="preserve">
    <value>The transaction item cannot be deleted because it is the last item on the transaction</value>
</data>
```

---

#### Modified: `Backend/PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`

```csharp
/// <summary>
///   Looks up a localized string similar to The transaction item cannot be deleted because it is the last item on the transaction.
/// </summary>
internal static string TransactionLastItem {
    get {
        return ResourceManager.GetString("TransactionLastItem", resourceCulture);
    }
}
```

---

#### Modified: `Backend/PocketAdvisor.Services/Constants/ErrorMetadataKeys.cs`

```csharp
/// <summary>
/// The metadata key that marks an error as a conflict.
/// </summary>
public const string Conflict = "Conflict";
```

---

#### Modified: `Backend/PocketAdvisor.Services/Interfaces/ITransactionService.cs`

```csharp
/// <summary>
/// Deletes the specified transaction and all of its items asynchronously.
/// </summary>
/// <param name="transactionId">The identifier of the transaction to delete.</param>
/// <param name="userId">The identifier of the currently authenticated user.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result" /> indicating the success or failure of the operation.
/// </returns>
Task<Result> DeleteTransactionAsync(Guid transactionId, Guid userId);

/// <summary>
/// Deletes a single item from the specified transaction asynchronously.
/// Fails with a conflict if the item is the last one on the transaction.
/// </summary>
/// <param name="transactionId">The identifier of the transaction.</param>
/// <param name="itemId">The identifier of the item to remove from the transaction.</param>
/// <param name="userId">The identifier of the currently authenticated user.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result" /> indicating the success or failure of the operation.
/// </returns>
Task<Result> DeleteTransactionItemAsync(Guid transactionId, Guid itemId, Guid userId);
```

---

#### Modified: `Backend/PocketAdvisor.Services/Implementations/TransactionService.cs`

```csharp
#region DeleteTransactionAsync

/// <inheritdoc />
public async Task<Result> DeleteTransactionAsync(Guid transactionId, Guid userId)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Deleting transaction '{TransactionId}'...", transactionId);
    }

    Transaction? transaction = await TransactionRepository.GetSingleOrDefaultAsync(
        t => t.Id == transactionId && (
            (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
            (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
        ),
        includes: [t => t.FromAccount!, t => t.ToAccount!]
    );

    if (transaction is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Transaction '{TransactionId}' was not found for user '{UserId}'.",
                transactionId,
                userId
            );
        }

        return Result.Fail(string.Empty);
    }

    await TransactionManager.Value.BeginTransactionAsync();

    TransactionRepository.Delete(transaction);

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation("Transaction '{TransactionId}' deleted successfully.", transactionId);
    }

    return Result.Ok();
}

#endregion

#region DeleteTransactionItemAsync

/// <inheritdoc />
public async Task<Result> DeleteTransactionItemAsync(Guid transactionId, Guid itemId, Guid userId)
{
    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation(
            "Deleting item '{ItemId}' from transaction '{TransactionId}'...",
            itemId,
            transactionId
        );
    }

    Transaction? transaction = await TransactionRepository.GetSingleOrDefaultAsync(
        t => t.Id == transactionId && (
            (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
            (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
        ),
        includes: [t => t.FromAccount!, t => t.ToAccount!]
    );

    if (transaction is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Transaction '{TransactionId}' was not found for user '{UserId}'.",
                transactionId,
                userId
            );
        }

        return Result.Fail(string.Empty);
    }

    TransactionItem? transactionItem = await TransactionItemRepository.GetSingleOrDefaultAsync(
        ti => ti.TransactionId == transactionId && ti.ItemId == itemId
    );

    if (transactionItem is null)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Item '{ItemId}' was not found on transaction '{TransactionId}'.",
                itemId,
                transactionId
            );
        }

        return Result.Fail(string.Empty);
    }

    bool hasOtherItems = await TransactionItemRepository.ExistsAsync(
        ti => ti.TransactionId == transactionId && ti.ItemId != itemId
    );

    if (!hasOtherItems)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Cannot delete item '{ItemId}' from transaction '{TransactionId}' as it is the last item.",
                itemId,
                transactionId
            );
        }

        Error error = new(ValidationMessages.TransactionLastItem)
        {
            Metadata =
            {
                [ErrorMetadataKeys.Conflict] = true
            }
        };

        return Result.Fail(error);
    }

    await TransactionManager.Value.BeginTransactionAsync();

    TransactionItemRepository.Delete(transactionItem);

    await TransactionManager.Value.CommitTransactionAsync();

    if (Logger.IsEnabled(LogLevel.Information))
    {
        Logger.LogInformation(
            "Item '{ItemId}' deleted from transaction '{TransactionId}' successfully.",
            itemId,
            transactionId
        );
    }

    return Result.Ok();
}

#endregion
```

---

#### Modified: `Backend/PocketAdvisor.WebApplication/Controllers/TransactionController.cs`

```csharp
#region DeleteTransactionAsync

/// <summary>
/// Deletes the specified transaction and all of its items for the currently authenticated user asynchronously.
/// </summary>
/// <param name="id">The identifier of the transaction to delete.</param>
[HttpDelete("{id:guid}")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
public async Task<IActionResult> DeleteTransactionAsync([FromRoute] Guid id)
{
    Result result = await Service.DeleteTransactionAsync(id, CurrentUserId);

    if (result.IsFailed)
    {
        return NotFound();
    }

    return NoContent();
}

#endregion

#region DeleteTransactionItemAsync

/// <summary>
/// Deletes a single item from the specified transaction for the currently authenticated user asynchronously.
/// Returns 409 Conflict if the item is the last one on the transaction.
/// </summary>
/// <param name="transactionId">The identifier of the transaction.</param>
/// <param name="itemId">The identifier of the item to remove from the transaction.</param>
[HttpDelete("{transactionId:guid}/items/{itemId:guid}")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(void), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(void), StatusCodes.Status409Conflict)]
public async Task<IActionResult> DeleteTransactionItemAsync([FromRoute] Guid transactionId,
    [FromRoute] Guid itemId)
{
    Result result = await Service.DeleteTransactionItemAsync(transactionId, itemId, CurrentUserId);

    if (result.IsFailed)
    {
        if (result.Errors.Any(e => e.Metadata.TryGetValue(ErrorMetadataKeys.Conflict, out _)))
        {
            return Conflict();
        }

        return NotFound();
    }

    return NoContent();
}

#endregion
```

### Design Decisions

- `Conflict` metadata key on `ErrorMetadataKeys` — reuses the existing error discrimination pattern (same mechanism
as `PropertyName` used to distinguish 400 vs 404) rather than introducing a separate result type or exception.
- Ownership check on the transaction only — verifying the transaction belongs to the user is sufficient for both
endpoints; no need to re-verify item ownership since `TransactionItem` has no independent owner.
- `ExistsAsync(ti => ... ti.ItemId != itemId)` for last-item check — avoids fetching a full list just to count; a
single `AnyAsync` is cheaper.
- `DeleteBehavior.Cascade` already configured on `TransactionItem → Transaction` — no explicit item deletion loop
needed in `DeleteTransactionAsync`.
- `using PocketAdvisor.Services.Constants` added to `TransactionService.cs` — needed for the new
`ErrorMetadataKeys.Conflict` reference.

---

## Turn 3

### User

> Export

### Assistant Thinking

Read `reference_export_format.md` from memory to confirm the exact format. Reconstructed the full conversation 
from context (transcript tool only sees sibling sessions, not the current one). Wrote to
`Prompts\transaction-delete-endpoints.md`.
