# Item Detail Page with Chart.js Spending History

**Date:** 2026-05-10

---

## Turn 1

### User

> How hard would it be to add Chart.js to the items. I would want the items to be clickable.
> And a chart should show how much did the user spend on it and when.

### Assistant Thinking

Explored the full project structure before answering. Read the key files:
- `item-list.ts` / `item-list.html` — items are rendered as a card grid; `.item-card` already has `cursor: pointer` in CSS
- `item-response.ts` — item has `id`, `name`, `unitCategory`
- `transaction-response.ts` / `transaction-item-response.ts` — transactions have `occurredAt`, `fromAccountId`, `toAccountId`, and a list of items with `itemId`, `totalPrice`, `amountValue`, `amountUnit`
- `transaction.ts` (service) — existing `getTransactions(accountId)` only filters by account, not by item
- `TransactionController.cs` — `GET /api/transactions?accountId=` is the only query shape
- `TransactionService.cs` — `GetTransactionsAsync` queries by account ID with `.Include(t => t.TransactionItems!)`

Key finding: there is no backend endpoint to fetch transactions by item. One would be needed. The user asked how hard — answered with a full breakdown: backend (1 new endpoint across 3 files), frontend (Chart.js install, new dialog or page, click wiring).

### Assistant Response

Gave a detailed feasibility assessment. Outlined:
- New backend endpoint needed: `GET /api/transactions?itemId=` touching `ITransactionService`, `TransactionService`, `TransactionController`
- Frontend: make cards clickable, open a Chart.js chart showing `occurredAt` vs `totalPrice`
- Flagged the cross-account complication: an item can appear in transactions across multiple accounts
- Estimated ~3–4 hours total, moderate effort

---

## Turn 2

### User

> The idea is good, but I want a dedicated page, not just a dialog.

### Assistant Thinking

Confirmed the routing pattern by reading:
- `app.routes.ts` — lazy-loaded module per feature area
- `items-routing-module.ts` — only had `{ path: '', component: ItemList }`
- `account-transactions.ts` / `account-transactions.html` — the existing detail-page pattern to mirror: `ActivatedRoute` for param, `forkJoin` for parallel data loading, back button, header with a stat, expandable transaction list

Asked one clarifying question: line vs bar chart. User chose line chart.

Planned the full implementation before writing anything:
- Backend: `GetTransactionsByItemAsync` on service + interface; controller updated to accept `?itemId=` as alternative query param
- Frontend: `chart.js` via CDN (npm registry blocked in sandbox); new route `items/:id`; click on card navigates; new `ItemDetail` component (TS + HTML + CSS)

### Assistant Response

Built everything across 10 files:

---

#### Modified: `Backend/PocketAdvisor.Services/Interfaces/ITransactionService.cs`

Added `GetTransactionsByItemAsync(Guid itemId, Guid userId)` to the interface.

---

#### Modified: `Backend/PocketAdvisor.Services/Implementations/TransactionService.cs`

```csharp
public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsByItemAsync(Guid itemId, Guid userId)
{
    IReadOnlyList<Transaction> transactions = await TransactionRepository.GetAllAsync(
        t => t.TransactionItems!.Any(ti => ti.ItemId == itemId) && (
            (t.FromAccountId.HasValue && t.FromAccount!.UserId == userId) ||
            (t.ToAccountId.HasValue && t.ToAccount!.UserId == userId)
        ),
        includes: q => q.Include(t => t.TransactionItems!)
    );
    // ... maps to TransactionResponse list
}
```

---

#### Modified: `Backend/PocketAdvisor.WebApplication/Controllers/TransactionController.cs`

Updated `GetTransactionsAsync` to accept either `?accountId=` or `?itemId=`, returning `400` if neither is provided:

```csharp
public async Task<IActionResult> GetTransactionsAsync([FromQuery] Guid? accountId, [FromQuery] Guid? itemId)
{
    if (accountId.HasValue) { ... return Ok(...); }
    if (itemId.HasValue)    { ... return Ok(...); }
    return BadRequest();
}
```

---

#### Modified: `Frontend/src/index.html`

Added Chart.js 4 CDN script (no npm install needed):

```html
<script src="https://cdn.jsdelivr.net/npm/chart.js@4/dist/chart.umd.min.js"></script>
```

---

#### Modified: `Frontend/src/app/core/services/transaction.ts`

```typescript
getTransactionsByItem(itemId: string): Observable<TransactionResponse[]> {
  return this.http.get<TransactionResponse[]>(`${this.base}`, {
    params: { itemId },
  });
}
```

---

#### Modified: `Frontend/src/app/items/items-routing-module.ts`

```typescript
const routes: Routes = [
  { path: '', component: ItemList },
  { path: ':id', component: ItemDetail }
];
```

---

#### Modified: `Frontend/src/app/items/item-list/item-list.ts`

Injected `Router`, added `openDetail(item)` method.

---

#### Modified: `Frontend/src/app/items/item-list/item-list.html`

Wired `(click)="openDetail(item)"` on `.item-card` with keyboard support (`keydown.enter`, `keydown.space`). The existing `$event.stopPropagation()` on the menu button prevents bubbling.

---

#### New: `Frontend/src/app/items/item-detail/item-detail.ts`

Full component. Key patterns:
- `forkJoin` over `getItems()` + `getTransactionsByItem(itemId)`
- `AfterViewInit` + `setTimeout(() => renderChart(), 0)` to ensure canvas is in DOM after `@if` resolves
- `ngOnDestroy` calls `chartInstance.destroy()`
- `declare const Chart: any` for CDN global
- Chart: line, x = formatted date labels, y = spend aggregated per day, primary-600-base colour

---

#### New: `Frontend/src/app/items/item-detail/item-detail.html`

Header with back button + "Total spent" stat, Chart.js `<canvas #chartCanvas>`, expandable transaction list below (same expand/collapse pattern as `account-transactions`).

---

#### New: `Frontend/src/app/items/item-detail/item-detail.css`

Mirrors `account-transactions.css` layout and naming conventions. All colours from CSS custom property palette.

### Design Decisions

- **CDN over npm for Chart.js:** npm registry is blocked in the sandbox environment. CDN script in `index.html` is functionally identical at runtime and keeps the build clean.
- **No date adapter:** Chart.js `time` scale requires a separate adapter package. Switched to plain string labels formatted via `toLocaleDateString` — zero extra dependencies, identical visual result.
- **Currency hardcoded to USD temporarily:** at this stage the page had no access to account data; noted as a known gap to fix in the next turn.
- **Controller: nullable params instead of separate endpoint:** keeping `GET /api/transactions` as the single resource URL and branching on query params preserves REST resource semantics (both shapes return the same `TransactionResponse[]` collection).

---

## Turn 3

### User

> The page is good, but the currency is displayed incorrectly.

### Assistant Thinking

The `CurrencyPipe` calls were all hardcoded to `'USD'`. The correct currency lives on `AccountResponse.currencyCode` (a numeric `CurrencyCode` enum with ISO 4217 numeric values, e.g. `Usd = 840`). The existing `account-transactions.ts` resolves it with `CurrencyCode[account.currencyCode].toUpperCase()`.

The item detail page crosses multiple accounts, so there is no single correct currency. Decided:
- Add `AccountService` to `forkJoin` to fetch all accounts
- Build `accountsById: Map<string, AccountResponse>` 
- `currencyAlpha(tx)` resolves via `tx.fromAccountId ?? tx.toAccountId`
- Header "Total spent" stat: only shown when all transactions share the same currency (`headerCurrencyAlpha` signal, `null` when mixed)
- Per-row amounts: always use `currencyAlpha(tx)`

### Assistant Response

Updated two files:

---

#### Modified: `Frontend/src/app/items/item-detail/item-detail.ts`

- Imported `AccountService`, `AccountResponse`, `CurrencyCode`
- Added `accountsById = new Map<string, AccountResponse>()`
- Added `headerCurrencyAlpha = signal<string | null>(null)`
- Added `currencyAlpha(tx): string` method
- Extended `forkJoin` to include `accounts: this.accountService.getAccounts()`
- After load: computes `new Set(sorted.map(tx => this.currencyAlpha(tx)))` — sets `headerCurrencyAlpha` only when size === 1

---

#### Modified: `Frontend/src/app/items/item-detail/item-detail.html`

- Header stat: `@if (...&& headerCurrencyAlpha())` — hidden when currencies are mixed
- `grandTotal | currency : headerCurrencyAlpha()! : ...`
- Per-row: `itemTotalForTx(tx) | currency : currencyAlpha(tx) : ...`
- Expanded item price: `txItem.totalPrice | currency : currencyAlpha(tx) : ...`

TypeScript check: `tsc --noEmit --skipLibCheck` — no errors.
