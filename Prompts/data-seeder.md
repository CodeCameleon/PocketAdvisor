# Data Seeder

**Date:** 2026-04-30

---

## Turn 1

### User

> I need to add test data to the application. What do you think is the best way to add these data when a fresh
> database is created?

### Assistant Thinking

Explored the project to understand the existing database setup. Found: ASP.NET Core (.NET 10), EF Core with
PostgreSQL, auto-migration on startup via `MigrateAsync()` in `Program.cs`, no existing seeding mechanism.
Considered three options: `HasData()` in `OnModelCreating`, a separate SQL script, and an `IDataSeeder`-style service
called from `Program.cs`.

### Assistant Response

Recommended a `DataSeeder` service called from `Program.cs` right after `MigrateAsync()`, gated to
`IsDevelopment()`. Reasons:

- `HasData()` bakes seed data into migrations — painful when test data changes frequently, every tweak generates a
new migration.
- A separate SQL script is disconnected from the app lifecycle and doesn't use EF entities.
- The `DataSeeder`-in-startup pattern uses existing EF models, runs automatically on a fresh DB, is idempotent
(checks `Users.AnyAsync()` first), and never runs in production.

---

## Turn 2

### User

> Yes, that would be nice. I need a regular and an admin user. 5-10 global categories. 2-5 personal categories
> for the regular user. Two account for the regular user, one for the bank and one for the cash. The regular user
> should also have like 10-15 items and like 20-30 transaction with varying amount of items on each.

### Assistant Thinking

Read all entity models (`User`, `Account`, `Category`, `Item`, `Transaction`, `TransactionItem`, `Token`,
`Quantity`), enums (`EUserRole`, `ECurrencyCode`, `EUnitCategory`, `EUnit`, `ETokenType`), `PocketAdvisorDbContext.cs`,
and `Program.cs`. Noted that `IPasswordHasher<User>` is already registered as scoped. Noted `Category.UserId` is
`required Guid?` so `null` is valid for global categories. Added `Microsoft.AspNetCore.Identity` package to
`PocketAdvisor.DbContexts.csproj` so the seeder can inject the hasher directly.

### Assistant Response

Created three files:

---

#### New: `Backend/PocketAdvisor.DbContexts/DataSeeder.cs`

```csharp
// Idempotent seeder — bails out early if Users table is non-empty.
// Saves in dependency order: Users → Accounts → Categories → Items → Transactions → TransactionItems.
// Uses IPasswordHasher<User> to hash passwords identically to UserService.
// Helpers: CreateUser, CreateTransaction, CreateTransactionItem (all static except CreateUser).
```

Seed data:
- **2 users**: `admin@pocketadvisor.dev` / `Admin1234!` (Administrator), `user@pocketadvisor.dev` / `User1234!`
(User), both email-verified.
- **8 global categories**: Groceries, Dining Out, Transportation, Housing, Healthcare, Entertainment, Clothing,
Education.
- **3 personal categories**: Side Projects, Gym & Sports, Subscriptions (all belonging to regular user).
- **2 accounts**: Main Bank Account (3,500 USD), Wallet (150 USD).
- **13 items**: Milk, Bread, Chicken Breast, Rice, Eggs, Olive Oil, Bus Ticket, Fuel, Streaming Service, Movie
Ticket, Gym Membership, Coffee, Lunch Meal.
- **25 transactions** spread over ~3 months, with 1–5 items each.

---

#### Modified: `Backend/PocketAdvisor.DbContexts/PocketAdvisor.DbContexts.csproj`

Added `Microsoft.AspNetCore.Identity` package reference.

---

#### Modified: `Backend/PocketAdvisor.WebApplication/Program.cs`

```csharp
// Registered DataSeeder as scoped service.
builder.Services.AddScoped<DataSeeder>();

// After MigrateAsync(), seeding is gated to Development environment.
if (app.Environment.IsDevelopment())
{
    DataSeeder seeder = serviceProvider.GetRequiredService<DataSeeder>();
    await seeder.SeedAsync();
}
```

---

## Turn 3

### User

> I have made some change to the DataSeeder. I want you to finish the correction that I started. Also watch out
> for the changed item unit categories and the changed account currency codes.

### Assistant Thinking

Read the updated file. User had started a refactor: switching from inline `Tx()`/`TI()` helper pattern to a
two-step `CreateTransaction` + `CreateTransactionItem` approach (transactions saved first, then items referencing
their IDs). Also changed: currency to HUF with realistic amounts, `Bread` → `EUnitCategory.Mass`,
`Gym Membership` / `Streaming Service` → `EUnitCategory.Time`, removed `Lunch Meal` item (replaced with
`Chicken Breast` + `Coffee` on restaurant transactions), and reorganised the class to use XML doc comments and
`#region` blocks. Noticed a compile error: `Day` local function was declared after an unreachable `return;`
statement, but referenced above it.

### Assistant Response

Completed the refactor:

- Converted all 25 transactions from old `Tx()`/`TI()` pattern to `CreateTransaction` + `CreateTransactionItem`.
- Updated all prices to HUF (e.g. fuel ~360 HUF/L, gym 3,500 HUF/month, streaming 1,399 HUF/month).
- `Bread` uses `EUnit.Gram` throughout; `Gym Membership` / `Streaming Service` use `EUnit.Month`.
- Removed `lunchMeal` references; restaurant transactions use `chicken` + `coffee`.
- Fixed `return;` / `Day` ordering bug — `Day` local function moved before the transactions list.

---

## Turn 4

### User

> Take a look at how the current data seeder class looks like, then add a similar create method for the account,
> items and categories too.

### Assistant Thinking

Read the current file. Existing helper methods used `#region` blocks with XML doc comments: `CreateTransaction`,
`CreateTransactionItem`, `CreateUser`. Needed to add `CreateAccount`, `CreateCategory`, `CreateItem` following
the same pattern. Also spotted the `return;` / `Day` bug had reappeared and fixed it again.

### Assistant Response

Added three new `#region` helper methods, inserted alphabetically before `#region CreateTransaction`:

---

#### Modified: `Backend/PocketAdvisor.DbContexts/DataSeeder.cs`

```csharp
#region CreateAccount
private static Account CreateAccount(string name, decimal balance, ECurrencyCode currencyCode, User user)
{
    Account account = new()
    {
        Name = name,
        Balance = balance,
        CurrencyCode = currencyCode,
        UserId = user.Id,
    };
    return account;
}
#endregion

#region CreateCategory
private static Category CreateCategory(string name, User? user)
{
    Category category = new()
    {
        Name = name,
        UserId = user?.Id,
    };
    return category;
}
#endregion

#region CreateItem
private static Item CreateItem(string name, EUnitCategory unitCategory, User user)
{
    Item item = new()
    {
        Name = name,
        UnitCategory = unitCategory,
        UserId = user.Id,
    };
    return item;
}
#endregion
```

`SeedAsync` updated to use all three helpers throughout.

---

## Turn 5

### User

> Good, next I want you to create a new item for each missing unit category (Length, Area, Energy, DataSize).
> And also create transactions for them

### Assistant Thinking

Existing unit categories covered: Mass, Uncategorized, Volume, Time. Missing: Length, Area, Energy, DataSize.
Chose realistic items: Fabric (Length), Flooring (Area), Electricity (Energy), Cloud Storage (DataSize). Picked
day values that fit the existing chronology: Day(50) Fabric, Day(48) Flooring, Day(30) Electricity, Day(20) Cloud
Storage. Assigned appropriate categories: Clothing, Housing, Housing, Subscriptions.

### Assistant Response

#### Modified: `Backend/PocketAdvisor.DbContexts/DataSeeder.cs`

Added 4 items (alphabetically sorted into the list):
```csharp
CreateItem("Cloud Storage", EUnitCategory.DataSize, regularUser),   // items[3]
CreateItem("Electricity", EUnitCategory.Energy, regularUser),        // items[6]
CreateItem("Fabric", EUnitCategory.Length, regularUser),             // items[7]
CreateItem("Flooring", EUnitCategory.Area, regularUser),             // items[8]
```

Added 4 transactions and their items:
```csharp
// 13 – Fabric purchase (cash, Clothing)
CreateTransactionItem(transactions[12], fabric, 3_200m, 2m, EUnit.Meter),
// 14 – Flooring purchase (bank, Housing)
CreateTransactionItem(transactions[13], flooring, 45_000m, 15m, EUnit.SquareMeter),
// 20 – Electricity bill (bank, Housing)
CreateTransactionItem(transactions[19], electricity, 18_500m, 250m, EUnit.KilowattHour),
// 24 – Cloud storage plan (bank, Subscriptions)
CreateTransactionItem(transactions[23], cloudStorage, 990m, 200m, EUnit.Gigabyte),
```

---

## Turn 6

### User

> You can ignore the return; problem please. Also I want you to put the new transactions into there proper spot,
> based on the Day(number) parameter, like the others.

### Assistant Thinking

Calculated the correct chronological insertion points for Day(50), Day(48), Day(30), Day(20) among the existing
Day values. Resulted in:
- Day(50) Fabric → index 12, between Day(56) and Day(45)
- Day(48) Flooring → index 13, right after Fabric
- Day(30) Electricity → index 19, between Day(35) and Day(28)
- Day(20) Cloud Storage → index 23, between Day(22) and Day(15)

All subsequent transaction indices in the `transactionItems` list updated accordingly.

### Assistant Response

#### Modified: `Backend/PocketAdvisor.DbContexts/DataSeeder.cs`

Reordered the transactions list and updated all `transactions[N]` indices in the transaction items list to match.
Final order (abbreviated):

```
Day(84) Day(82) Day(80) Day(77) Day(75) Day(74) Day(70) Day(68) Day(63)
Day(61) Day(59) Day(56) Day(50) Day(48) Day(45) Day(44) Day(42) Day(39)
Day(35) Day(30) Day(28) Day(25) Day(22) Day(20) Day(15) Day(14) Day(12)
Day(7) Day(1)
```

---

## Turn 7

### User

> Great, now I want you to add some transfers between the two accounts. 3 should be good, 2 one way and 1 to the other.

### Assistant Thinking

Transfers require both `FromAccountId` and `ToAccountId` set. None of the existing categories fit a transfer
semantically, so added a "Transfers" personal category. Chose day values: Day(55) bank→wallet, Day(32) bank→wallet,
Day(10) wallet→bank. Transfers have no items naturally — left them item-free and noted this in comments.

### Assistant Response

#### Modified: `Backend/PocketAdvisor.DbContexts/DataSeeder.cs`

Added `"Transfers"` to personal categories (`personalCategories[3]`), with `transfers` alias.

Added 3 transfer transactions in chronological order:
```csharp
// 13 – Transfer: bank to wallet
CreateTransaction(Day(55), transfers, bankAccount, cashAccount),
// 21 – Transfer: bank to wallet
CreateTransaction(Day(32), transfers, bankAccount, cashAccount),
// 30 – Transfer: wallet to bank
CreateTransaction(Day(10), transfers, cashAccount, bankAccount),
```

All subsequent indices updated. Transfer transactions noted as having no items in the `transactionItems` list.

---

## Turn 8

### User

> The transfer transactions should have a single item on them, that represents the money. A transaction should
> always have at least a single item on it.

### Assistant Thinking

A transfer item needs a unit. Since it represents money moving between accounts, `EUnitCategory.Uncategorized`
with `EUnit.Piece` at quantity 1 is the cleanest fit — the `TotalPrice` represents the transferred amount.
"Transfer" sorts alphabetically after "Streaming Service" so goes at `items[16]`. Note: by this point the user
had also refactored the file — moved to `Implementations/` subfolder, added `IDataSeeder` interface,
`CancellationToken` parameters, and made Transfers a global category instead of personal.

### Assistant Response

#### Modified: `Backend/PocketAdvisor.DbContexts/Implementations/DataSeeder.cs`

Added `"Transfer"` item:
```csharp
CreateItem("Transfer", EUnitCategory.Uncategorized, regularUser)  // items[16]
```

Added one transaction item per transfer:
```csharp
// 13 – Transfer: bank to wallet
CreateTransactionItem(transactions[12], transfer, 20_000m, 1m, EUnit.Piece),
// 21 – Transfer: bank to wallet
CreateTransactionItem(transactions[20], transfer, 15_000m, 1m, EUnit.Piece),
// 30 – Transfer: wallet to bank
CreateTransactionItem(transactions[29], transfer, 5_000m, 1m, EUnit.Piece),
```

### Design Decisions

- Transfer amounts chosen to be plausible given the account balances (Bank: 350,000 HUF, Wallet: 15,000 HUF):
20,000 and 15,000 HUF from bank to wallet, 5,000 HUF back.
- `EUnit.Piece` with quantity 1 used for transfers — the `TotalPrice` field carries the monetary value, which is
the only meaningful data for a transfer line item.
- "Transfer" item uses `EUnitCategory.Uncategorized` since it does not represent a physical quantity.

---

## Turn 9

### User

> Export

### Assistant Thinking

Triggered the Export macro. Read the export format reference and the final state of `DataSeeder.cs` from disk
(found at `Implementations/DataSeeder.cs` after the user's refactor moved it there). Wrote the full conversation as
a `.md` file to `Prompts\data-seeder.md`.
