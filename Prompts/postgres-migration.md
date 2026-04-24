# PostgreSQL Migration

**Date:** 2026-04-24

---

## 1. Orphan Token Records

**Adam:**
> I think I have found something strange. The Token database entity is set to cascade on the user deletion, but there
> are orphan records in the Token table. I think the corresponding users have been deleted using the database terminal.

**Thinking:**
The cascade is defined at the EF Core ORM level using `OnDelete(DeleteBehavior.Cascade)`. SQLite does not enforce
foreign key constraints by default — it requires `PRAGMA foreign_keys = ON` on each connection. When users are
deleted directly via the database terminal (bypassing EF Core's change tracker), SQLite simply removes the User
rows without cascading, leaving Token rows pointing at non-existent `UserId` values.

**Response:**
The root cause is that the cascade is defined at the EF Core level, but SQLite doesn't enforce foreign key
constraints by default. When users were deleted directly via the database terminal (bypassing EF Core), SQLite
just deleted the User rows without cascading — leaving Token rows pointing to non-existent `UserId` values.

Specifically:
- `OnDelete(DeleteBehavior.Cascade)` in EF Core only works when deletions go *through* EF Core's change tracker.
- SQLite requires `PRAGMA foreign_keys = ON` to enforce FK constraints at the DB level — without it, direct SQL
deletes don't cascade.
- Since the database uses `EnsureCreated()` (no migrations), there's no guarantee SQLite's FK enforcement pragma
was ever set.

**Two things to fix:**

**1. Enable SQLite foreign key enforcement at the connection level:**

```csharp
// In Program.cs, after building the app
using (var connection = new SqliteConnection("Data Source=../database.db"))
{
    connection.Open();
    var command = connection.CreateCommand();
    command.CommandText = "PRAGMA foreign_keys = ON;";
    command.ExecuteNonQuery();
}
```

**2. Clean up the orphan records now** — run this directly in the DB terminal:

```sql
PRAGMA foreign_keys = ON;

DELETE FROM Tokens
WHERE UserId NOT IN (SELECT Id FROM Users);
```

The same risk applies to `Accounts`, `Categories`, and `Items` tables since they all use the same cascade pattern
— worth running a similar orphan check on those too.

---

## 2. Would EF Migrations Fix This?

**Adam:**
> Would creating a migrations with EF and using that to create the database solve this problem?

**Thinking:**
Migrations generate explicit SQL with `FOREIGN KEY ... ON DELETE CASCADE` baked into the schema. However, even
with a proper FK constraint declared in the schema, SQLite still won't enforce it unless `PRAGMA foreign_keys = ON`
is set for that connection. So migrations alone are not sufficient — the pragma is the critical piece.

**Response:**
Yes, but only partially — and it's worth understanding exactly what migrations do and don't fix here.

**What migrations would fix:** Migrations generate explicit `CREATE TABLE` SQL with `FOREIGN KEY` constraints baked
in, emitting something like:

```sql
FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
```

This is a real database-level constraint, not just ORM logic.

**What migrations would NOT fix:** The FK constraint existing in the schema doesn't mean SQLite will actually enforce
it. SQLite's foreign key enforcement is still off by default, regardless of how the schema was created. So even with
a perfect migration, someone could still open the DB terminal and delete users without the cascade firing — unless
`PRAGMA foreign_keys = ON` is set for that connection too.

**So the real fix is two things together:**
1. Enable `PRAGMA foreign_keys = ON` on every connection — this is what actually enforces cascade behavior at the
database level, for any client including direct terminal access.
2. Migrations (or at minimum `EnsureCreated`) to make sure the FK constraint is declared in the schema.

Switching to migrations is still a good idea independently — it gives you a proper audit trail of schema changes
and avoids the fragility of `EnsureCreated()`, which silently does nothing if the database already exists. But the
pragma is the critical piece for preventing orphans from direct DB access.

---

## 3. Switching to PostgreSQL with Docker

**Adam:**
> And what do you think about switching to PostgreSQL using Docker?

**Thinking:**
PostgreSQL enforces FK constraints by default on every connection — no pragma required. This sidesteps the SQLite
problem entirely. Docker is a natural fit: a `docker-compose.yml` with a Postgres container alongside the API makes
local development reproducible and mirrors a real production setup. The migration involves swapping `UseSqlite()` for
`UseNpgsql()`, updating the connection string, and re-scaffolding migrations.

**Response:**
It's a solid upgrade for several reasons, and it sidesteps the SQLite foreign key problem entirely since PostgreSQL
enforces FK constraints by default on every connection — no pragma required. Direct terminal deletes would cascade
automatically.

**Where you'd gain:**
- Proper concurrent write handling — SQLite serializes all writes
- Richer data types — native UUIDs, enums, timestamps with timezone, JSON, arrays
- Better query planner and indexing
- Production-grade tooling — backups, replication, connection pooling with PgBouncer

**The Docker angle:** Docker is a natural fit. You'd define a `docker-compose.yml` with a Postgres container
alongside your API, making local development reproducible across machines and mirroring a real production
setup closely.

**What the migration involves:**
- Swap `UseSqlite()` for `UseNpgsql()` (Npgsql is the standard EF Core provider)
- Update the connection string
- Re-scaffold your migrations
- Delete the old `database.db` file

Overall it's a worthwhile move if you're planning to take this beyond a local project.

---

## 4. Transfer to PostgreSQL — NuGet Package and Connection String

**Adam:**
> Can you help me with the transfer to PostgreSQL? First I want you to take a look at the entities, can fix this
> inside the C# project, like changing the NuGet package and the connection string.

**Thinking:**
Four changes needed:
1. `PocketAdvisor.DbContexts.csproj` — swap `Microsoft.EntityFrameworkCore.Sqlite` for
`Npgsql.EntityFrameworkCore.PostgreSQL`
2. `ServiceCollectionExtensions.cs` — swap `UseSqlite()` for `UseNpgsql()`
3. `PocketAdvisorDbContext.cs` — the `IntegerType` constant is `"INTEGER"` (SQLite syntax); PostgreSQL uses `"bigint"`
4. `Program.cs` — replace `EnsureCreated()` with `MigrateAsync()` since migrations will be used

**Changes made:**

`PocketAdvisor.DbContexts.csproj` — swapped package:
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

`ServiceCollectionExtensions.cs` — swapped provider:
```csharp
options.UseNpgsql(connectionString);
```

`PocketAdvisorDbContext.cs` — fixed column type constant:
```csharp
private const string IntegerType = "bigint";
```

`Program.cs` — replaced database initialization:
```csharp
// Applies any pending migrations to the database.
using (IServiceScope scope = app.Services.CreateScope())
{
    IServiceProvider serviceProvider = scope.ServiceProvider;
    PocketAdvisorDbContext context = serviceProvider.GetRequiredService<PocketAdvisorDbContext>();
    await context.Database.MigrateAsync();
}
```

**Next steps:**
1. Run `dotnet restore` to pull down the Npgsql package
2. Update connection string to PostgreSQL format
3. Create first migration: `dotnet ef migrations add InitialCreate --project PocketAdvisor.DbContexts --startup-project
PocketAdvisor.WebApplication`
4. Set up Docker container

---

## 5. Switch to `numeric` Type for Decimal Properties

**Adam:**
> I want to use the numeric type for the decimal properties, like the Account.Balance, TransactionItem.TotalPrice and
> AmountValue. I want them as with the attribute inside the classes.

**Thinking:**
The `[Column(TypeName = "numeric(18,2)")]` attribute works on entity properties. For `Quantity.Value` (a value object
owned via `OwnsOne`), `[Column]` won't work directly — the right attribute is `[Precision(18, 6)]` since EF Core 6+
supports it as a data annotation and EF Core respects it on owned type properties. The precision of 6 matches the
`Precision = 6` constant already in `Quantity` used for comparisons and hashing. The `DecimalToLongConverter`,
`IntegerType` constant, and all `.HasConversion()` / `.HasColumnType()` Fluent API calls can be removed from
the DbContext.

**Changes made:**

`Account.cs` — added `[Column(TypeName = "numeric(18,2)")]` on `Balance`:
```csharp
[Column(TypeName = "numeric(18,2)")]
public required decimal Balance { get; set; }
```

`TransactionItem.cs` — added `[Column(TypeName = "numeric(18,2)")]` on `TotalPrice`:
```csharp
[Column(TypeName = "numeric(18,2)")]
public required decimal TotalPrice { get; set; }
```

`Quantity.cs` — added `[Precision(18, 6)]` on `Value`:
```csharp
[Precision(18, 6)]
public decimal Value { get; }
```

`PocketAdvisorDbContext.cs` — removed `DecimalToLongConverter`, `IntegerType` constant, and all `.HasConversion()`
/ `.HasColumnType()` Fluent API calls.

---

## 6. Switch to `[Precision]` Everywhere

**Adam:**
> I have decided to use the Precision attribute everywhere. Can you check if it is good?

**Thinking:**
Checked all three files. The user had already switched `Account.Balance` and `TransactionItem.TotalPrice` to
`[Precision(18, 2)]` and `Quantity.Value` to `[Precision(18, Scale)]` using the existing `Scale` constant
(renamed from `Precision` to avoid confusion with the attribute name). The `using Microsoft.EntityFrameworkCore`
import is present in all three files. One minor coupling concern: `Quantity` is a value object that now carries an
EF Core attribute, pulling `Microsoft.EntityFrameworkCore` as a dependency. Acceptable tradeoff for a project
structured like this.

**Response:**
Everything looks good. A few things worth pointing out:

**What's correct:**
- `[Precision(18, 2)]` on `Account.Balance` and `TransactionItem.TotalPrice` — right for monetary values
- `[Precision(18, Scale)]` on `Quantity.Value` using the `Scale` constant — clean, and the constant is already used
consistently throughout `Equals`, `GetHashCode`, and `CompareTo`, so the DB precision is guaranteed to stay in sync
with the comparison logic
- The `[Precision]` attribute lives in `Microsoft.EntityFrameworkCore`, which is correctly imported in all three files

**One thing to be aware of:** `[Precision]` on `Quantity.Value` will only take effect because EF Core picks it up
through the `OwnsOne` mapping in the DbContext. `Quantity` now carries an EF Core attribute on it — minor coupling
concern if `Quantity` is ever used outside the EF context, but a very common and acceptable tradeoff.

Everything else is solid. You're good to proceed with scaffolding the migration.

---

## 7. Create Docker Compose File

**Adam:**
> Now I want you to create a docker file that will house the database. It should work in both production and
> development. I'm not sure if you can pull it off without any data loss.

**Thinking:**
No risk of data loss from the Docker setup itself — the key is a named volume. PostgreSQL stores its data files
inside a Docker-managed volume on the host machine, not inside the container. The container is ephemeral; the volume
is not. You can stop, delete, and recreate the container as many times as you want and the data survives. The only
way to lose data is to explicitly run `docker volume rm`.

Two files needed: `docker-compose.yml` and `.env.example`. Connection strings in appsettings also need updating.

**Files created:**

`docker-compose.yml`:
```yaml
services:
  db:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-pocketadvisor}
      POSTGRES_USER: ${POSTGRES_USER:-pocketadvisor}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - db_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-pocketadvisor} -d ${POSTGRES_DB:-pocketadvisor}"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  db_data:
```

`.env.example`:
```env
POSTGRES_DB=pocketadvisor
POSTGRES_USER=pocketadvisor
POSTGRES_PASSWORD=your_strong_password_here
POSTGRES_PORT=5432
```

`.gitignore` updated to include `.env`.

`appsettings.json` and `appsettings.Development.json` connection strings updated to PostgreSQL format.

**Data safety explanation:** The `db_data` named volume lives on the host independently of the container lifecycle.
`restart: unless-stopped` means it survives machine reboots. To start: copy `.env.example` to `.env`, fill in a real
password, then run `docker compose up -d`.

---

## 8. Split Connection String — Secrets for Credentials

**Adam:**
> First, I want you to make some changes to the connection string. Only the Host, Port and Database should come
> from the appsettings.json files. The Username and password should be stored inside the secrets.json. The
> GetDefaultConnectionString() method should be updated to combine the values from the IConfiguration, since
> the appsettings and secrets value have already been added to the configuration by then.

**Thinking:**
The secrets are loaded into `IConfiguration` as flat key-value pairs via `AddInMemoryCollection` (seen in
`WebApplicationBuilderExtensions.cs`). The cleanest approach is to store credentials under
`ConnectionStrings:DefaultUsername` and `ConnectionStrings:DefaultPassword` in the secrets store — keeping everything
connection-string-related under the same section and following the existing pattern. `GetDefaultConnectionString`
needs to read all five parts and assemble the final string.

**Changes made:**

`appsettings.json` and `appsettings.Development.json` — replaced single `Default` connection string with three
safe-to-commit keys:
```json
"ConnectionStrings": {
  "DefaultHost": "localhost",
  "DefaultPort": "5432",
  "DefaultDatabase": "pocketadvisor"
}
```

`ConfigurationExtensions.cs` — added five new constants and rewrote `GetDefaultConnectionString`:
```csharp
public static string GetDefaultConnectionString(this IConfiguration configuration)
{
    IConfigurationSection section = configuration.GetSection(ConnectionStrings);

    string? host = section.GetValue<string>(DefaultHost);
    if (string.IsNullOrWhiteSpace(host))
        throw CreateInvalidOperationException(ConnectionStrings, DefaultHost);

    string? port = section.GetValue<string>(DefaultPort);
    if (string.IsNullOrWhiteSpace(port))
        throw CreateInvalidOperationException(ConnectionStrings, DefaultPort);

    string? database = section.GetValue<string>(DefaultDatabase);
    if (string.IsNullOrWhiteSpace(database))
        throw CreateInvalidOperationException(ConnectionStrings, DefaultDatabase);

    string? username = section.GetValue<string>(DefaultUsername);
    if (string.IsNullOrWhiteSpace(username))
        throw CreateInvalidOperationException(ConnectionStrings, DefaultUsername);

    string? password = section.GetValue<string>(DefaultPassword);
    if (string.IsNullOrWhiteSpace(password))
        throw CreateInvalidOperationException(ConnectionStrings, DefaultPassword);

    return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
}
```

**What to add to the secrets store:**
```
ConnectionStrings:DefaultUsername  →  pocketadvisor
ConnectionStrings:DefaultPassword  →  your_strong_password_here
```

---

## 9. Remove Default Values from Docker Compose

**Adam:**
> I have added the .env file with the real values. Can you make some changes to the compose file, I don't want to
> have any default values there.

**Thinking:**
Switch all `:-` (default value) syntax to `:?` (required with error message) for `POSTGRES_DB`, `POSTGRES_USER`,
and `POSTGRES_PASSWORD`. The port variable also needs changing. However, spaces are not allowed in the ports mapping
format, so error messages in that position must use underscores.

**Change made** — all variables switched to `:?` required syntax:
```yaml
environment:
  POSTGRES_DB: ${POSTGRES_DB:?POSTGRES_DB is required}
  POSTGRES_USER: ${POSTGRES_USER:?POSTGRES_USER is required}
  POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}
ports:
  - "${POSTGRES_PORT:?POSTGRES_PORT_is_required}:5432"
```

---

## 10. Fix — Spaces Not Allowed in Port Mapping

**Adam:**
> I think you made a mistake. Spaces are not allowed in ports mapping format.

**Thinking:**
The `:?` error message syntax with spaces breaks the ports mapping format in Docker Compose. The fix is to use
underscores in the error message instead.

**Fix applied:**
```yaml
- "${POSTGRES_PORT:?POSTGRES_PORT_is_required}:5432"
```

---

## 11. Revert Port to Default Value

**Adam:**
> You know what, remove the requirement for the port and add back the default value.

**Change made:**
```yaml
- "${POSTGRES_PORT:-5432}:5432"
```

`POSTGRES_PORT` defaults to `5432` if not set. The three database credentials remain required.

---

## 12. Simplify Healthcheck Variable Syntax

**Adam:**
> Is the required part in the healthcheck needed, because the variables are already checked inside the
> environment part?

**Thinking:**
By the time the healthcheck runs, Docker has already validated the environment variables and the container is up.
If `POSTGRES_USER` or `POSTGRES_DB` were missing, it would have failed before the healthcheck ever executed.
Plain variable references are sufficient.

**Change made:**
```yaml
test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
```

---

## Final State of Key Files

### `docker-compose.yml`
```yaml
services:
  db:
    image: postgres:17-alpine
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:?POSTGRES_DB is required}
      POSTGRES_USER: ${POSTGRES_USER:?POSTGRES_USER is required}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD is required}
    ports:
      - "${POSTGRES_PORT:-5432}:5432"
    volumes:
      - db_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER} -d ${POSTGRES_DB}"]
      interval: 10s
      timeout: 5s
      retries: 5

volumes:
  db_data:
```

### `appsettings.json` (ConnectionStrings section)
```json
"ConnectionStrings": {
  "DefaultHost": "localhost",
  "DefaultPort": "5432",
  "DefaultDatabase": "pocketadvisor"
}
```

### `PocketAdvisor.DbContexts.csproj` (relevant package)
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" />
```

### `ServiceCollectionExtensions.cs`
```csharp
options.UseNpgsql(connectionString);
```
