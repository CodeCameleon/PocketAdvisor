# Transaction Items Include Fix

**Date:** 2026-04-30

---

## Turn 1

### User

> I have found a bug in the GetAccountsAsync service method. The transaction items are never loaded.
> So they currently have no effect in the result. You might need to update the GetAllAsync method to fix this.

### Assistant Thinking

- Read `AccountService.cs` — `GetAccountsAsync` calls `GetAllAsync` with flat includes for `IncomingTransactions`
and `OutgoingTransactions`, then tries to `.SelectMany(t => t.TransactionItems ?? [])` on each. Since
`TransactionItems` is never included, it's always `null`, so the sum is always `0`.
- Read `BaseRepository.cs` — `GetAllAsync` accepts `IEnumerable<Expression<Func<TEntity, object>>>?` includes and
iterates them with `.Include()`. No mechanism for `ThenInclude`.
- Read `IBaseRepository.cs` — same signature on the interface.
- Read `Transaction.cs`, `Account.cs`, `TransactionItem.cs` to confirm the navigation property chain:
`Account → List<Transaction> → List<TransactionItem>`.
- Root cause: EF Core's `ThenInclude` requires a chained call off the typed `IIncludableQueryable<TEntity, TProperty>`
returned by `Include`. The existing `IEnumerable<Expression<Func<TEntity, object>>>` API can only express flat
single-level includes — it has no way to reach `TransactionItems` as a nested include off `IncomingTransactions` /
`OutgoingTransactions`.
- Fix strategy: add an optional `Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryBuilder` parameter to
`GetAllAsync` that the caller uses to chain arbitrary `Include`/`ThenInclude` calls. Applied after the flat
`includes`, before `AsSplitQuery`.

### Assistant Response

Three files changed.

---

#### Modified: `Backend/PocketAdvisor.Repositories/Interfaces/IBaseRepository.cs`

Added `queryBuilder` parameter to `GetAllAsync`:

```csharp
Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
    bool asSplitQuery = false, IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryBuilder = null,
    CancellationToken cancellationToken = default);
```

---

#### Modified: `Backend/PocketAdvisor.Repositories/Implementations/BaseRepository.cs`

Implemented the new parameter — applied after flat includes:

```csharp
public async Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
    bool asSplitQuery = false, IEnumerable<Expression<Func<TEntity, object>>>? includes = null,
    Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryBuilder = null,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(predicate);

    IQueryable<TEntity> query = Entities.AsQueryable();

    if (includes is not null)
    {
        query = includes.Aggregate(
            query,
            (current, include) => current.Include(include)
        );
    }

    if (queryBuilder is not null)
    {
        query = queryBuilder(query);
    }

    if (asSplitQuery)
    {
        query = query.AsSplitQuery();
    }

    List<TEntity> entities = await query.Where(predicate).ToListAsync(cancellationToken);
    ...
}
```

---

#### Modified: `Backend/PocketAdvisor.Services/Implementations/AccountService.cs`

Added `using Microsoft.EntityFrameworkCore` and passed a `queryBuilder` to load `TransactionItems`:

```csharp
IReadOnlyList<Account> accounts = await AccountRepository.GetAllAsync(
    a => a.UserId == userId,
    asSplitQuery: true,
    [
        a => a.IncomingTransactions!,
        a => a.OutgoingTransactions!
    ],
    queryBuilder: q => q
        .Include(a => a.IncomingTransactions!)
            .ThenInclude(t => t.TransactionItems!)
        .Include(a => a.OutgoingTransactions!)
            .ThenInclude(t => t.TransactionItems!)
);
```

---

### Design Decisions

- The flat `includes` array was kept alongside `queryBuilder` for backward compatibility — all existing callers
of `GetAllAsync` that only need flat includes continue to work unchanged.
- `queryBuilder` is applied after `includes` so the two can compose without interfering.

---

## Turn 2

### User

> I don't like it like this. Can you combine somehow the include and the queryBuilder part like into an
> enumerable of IIncludableQueryable<> or something like that?

### Assistant Thinking

- Researched `IIncludableQueryable<TEntity, TProperty>` via an Explore agent — confirmed it is in
`Microsoft.EntityFrameworkCore.Query` and implements `IQueryable<TEntity>`.
- Key constraint: `TProperty` changes with every `.ThenInclude` call in a chain. After
`.Include(a => a.IncomingTransactions!)` the type is `IIncludableQueryable<Account, Transaction>`; after
`.ThenInclude(t => t.TransactionItems!)` it becomes `IIncludableQueryable<Account, TransactionItem>`. There is no
shared generic base you can collect into an enumerable without erasing to `object` — which breaks EF Core's
extension methods.
- Conclusion: `IEnumerable<IIncludableQueryable<TEntity, ?>>` is not feasible with full type safety.
- Best alternative: collapse both `includes` and `queryBuilder` into a single `Func<IQueryable<TEntity>,
IQueryable<TEntity>>? includes`. This handles flat and nested includes identically, removes all redundancy, and is
the idiomatic EF Core pattern. Applied the same change to `GetSingleOrDefaultAsync` for consistency.

### Assistant Response

The separate `includes` enumerable and `queryBuilder` delegate were merged into a single `Func<IQueryable<TEntity>,
IQueryable<TEntity>>? includes` parameter on both `GetAllAsync` and `GetSingleOrDefaultAsync`.

---

#### Modified: `Backend/PocketAdvisor.Repositories/Interfaces/IBaseRepository.cs`

```csharp
Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
    bool asTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
    bool asSplitQuery = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
    CancellationToken cancellationToken = default);
```

---

#### Modified: `Backend/PocketAdvisor.Repositories/Implementations/BaseRepository.cs`

```csharp
public async Task<TEntity?> GetSingleOrDefaultAsync(Expression<Func<TEntity, bool>> predicate,
    bool asTracking = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
    CancellationToken cancellationToken = default)
{
    ...
    if (includes is not null)
    {
        query = includes(query);
    }
    ...
}

public async Task<IReadOnlyList<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>> predicate,
    bool asSplitQuery = false, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null,
    CancellationToken cancellationToken = default)
{
    ...
    if (includes is not null)
    {
        query = includes(query);
    }

    if (asSplitQuery)
    {
        query = query.AsSplitQuery();
    }
    ...
}
```

---

#### Modified: `Backend/PocketAdvisor.Services/Implementations/AccountService.cs`

Call site simplified — the redundant flat array is gone:

```csharp
IReadOnlyList<Account> accounts = await AccountRepository.GetAllAsync(
    a => a.UserId == userId,
    asSplitQuery: true,
    q => q
        .Include(a => a.IncomingTransactions!)
            .ThenInclude(t => t.TransactionItems!)
        .Include(a => a.OutgoingTransactions!)
            .ThenInclude(t => t.TransactionItems!)
);
```

---

### Design Decisions

- `IEnumerable<IIncludableQueryable<TEntity, TProperty>>` was ruled out: `TProperty` is a different concrete type at
each step of a chain (e.g. `Transaction` after `Include`, `TransactionItem` after `ThenInclude`), so there is no
single generic parameter that works across a full chain without erasing type information and breaking EF Core's
strongly-typed extension methods.
- The `Func<IQueryable<TEntity>, IQueryable<TEntity>>` delegate is the standard EF Core pattern for composable,
type-safe include chains — flat or nested, one or many levels deep, with full IntelliSense support.
- `GetSingleOrDefaultAsync` was updated alongside `GetAllAsync` to keep the API consistent across both read methods.
