# PocketAdvisor — Project Documentation

## Table of Contents

1. [Project Overview](#1-project-overview)
2. [Technology Stack](#2-technology-stack)
   - 2.1 [Backend](#21-backend)
   - 2.2 [Frontend](#22-frontend)
   - 2.3 [Infrastructure](#23-infrastructure)
3. [Architecture](#3-architecture)
   - 3.1 [Backend Architecture](#31-backend-architecture)
   - 3.2 [Frontend Architecture](#32-frontend-architecture)
4. [Data Model](#4-data-model)
5. [API Reference](#5-api-reference)
6. [Authentication & Security](#6-authentication--security)
7. [Functional Requirements](#7-functional-requirements)
8. [Non-Functional Requirements](#8-non-functional-requirements)

---

## 1. Project Overview

PocketAdvisor is a personal finance management web application that allows users to track their income, expenses, and transfers across multiple accounts. Users can categorize transactions, attach trackable items with physical quantities to each transaction, and inspect spending history on a per-item basis through interactive charts.

The application supports two user roles:

- **User** — the standard account holder who manages their own accounts, categories, items, and transactions.
- **Administrator** — a privileged operator who can create, rename, and delete global categories available to all users.

---

## 2. Technology Stack

### 2.1 Backend

| Concern | Choice | Reasoning |
|---|---|---|
| Runtime | **.NET 10 (ASP.NET Core)** | .NET's mature async model, first-class dependency injection, and strongly-typed configuration make it well-suited for building REST APIs. .NET 10 is the latest LTS-aligned release. |
| Language | **C# 13** | Strongly typed, with modern features (records, required members, pattern matching) that improve clarity and reduce bugs. |
| ORM | **Entity Framework Core 10 (EF Core)** | Code-first migrations, LINQ-based queries, and change tracking remove the need to write raw SQL for standard CRUD, while still allowing raw queries when needed. |
| Database provider | **Npgsql for EF Core** | The official, well-maintained PostgreSQL driver for EF Core; chosen because the database is PostgreSQL. |
| Validation | **FluentValidation 12** | Declarative, chainable validators that live in dedicated classes, keeping controllers and services clean. Integrates seamlessly with ASP.NET Core's dependency injection and `ValidationProblemDetails` error format. |
| Result handling | **FluentResults 4** | A functional-style `Result<T>` / `Result` wrapper that avoids exception-driven control flow for expected business errors (not-found, conflict, invalid input). Errors carry structured metadata so controllers can map them to the correct HTTP status code without conditional logic scattered across the codebase. |
| Authentication | **Microsoft JWT Bearer (JwtBearer 10)** | Standard bearer-token authentication baked into ASP.NET Core; avoids third-party auth servers for a university-scale project while still being industry-standard. |
| Password hashing | **ASP.NET Core Identity `PasswordHasher<T>`** | Provides PBKDF2-based hashing with a built-in rehash path (`SuccessRehashNeeded`), so password security can be upgraded transparently without breaking existing sessions. |
| Email delivery | **Resend SDK** | Resend offers template-based transactional email with a minimal SDK. Templates for email-verification and password-reset are maintained in the Resend dashboard and referenced by GUID, keeping HTML out of source control. |
| Secrets management | **SecureStore** | Sensitive configuration values (JWT signing key, token HMAC secrets, Resend API key) are stored in an encrypted `secrets.bin` file rather than plain `appsettings.json` or environment variables, reducing accidental exposure. |
| API documentation | **Swashbuckle / Swagger** | Auto-generates an OpenAPI spec from controller attributes, giving developers a browsable, testable API surface with zero extra effort. |

### 2.2 Frontend

| Concern | Choice | Reasoning |
|---|---|---|
| Framework | **Angular 21** | A full-featured, opinionated framework that provides routing, forms, HTTP client, dependency injection, and a component model out of the box — reducing the number of individual library decisions. Its module and lazy-loading system directly maps to PocketAdvisor's feature areas (accounts, categories, items, admin). |
| Language | **TypeScript 5.9** | Catches type errors at compile time, making it easier to keep frontend models in sync with backend DTOs. |
| UI component library | **Angular Material 21** | Google's officially supported Material Design component library for Angular. Provides accessible, polished components (dialogs, tables, form fields, buttons, icons) that accelerate development and enforce visual consistency. Chosen because it integrates natively with Angular's change detection, animations, and CDK. |
| HTTP client | **Angular `HttpClient` with Fetch API** | The native Angular HTTP client, configured with `withFetch()` to use the modern browser Fetch API instead of `XMLHttpRequest`, giving better streaming and abort support. |
| Reactive state | **Angular Signals** | Used for component-level state (`signal()`, `computed()`) to avoid unnecessary re-renders and make reactivity explicit without introducing a third-party state library. |
| Session persistence | **`localStorage`** | JWT and refresh tokens are stored in `localStorage` so that sessions survive page refreshes. The session interceptor reads from this store on every request. |
| Charts | **Chart.js (CDN)** | A lightweight, canvas-based charting library loaded via a CDN `<script>` tag in `index.html`. Avoids bundling Chart.js into the main chunk — reducing initial download size — while still being available globally via `declare const Chart: any`. |
| Code formatting | **Prettier** | Enforces a consistent code style across all TypeScript, HTML, and CSS files with no per-developer configuration drift. |

### 2.3 Infrastructure

| Concern | Choice | Reasoning |
|---|---|---|
| Database | **PostgreSQL 17 (Alpine)** | A robust, open-source relational database. The Alpine image keeps the container footprint small. PostgreSQL's support for `uuid` primary keys, precise `numeric` types, and JSON fits the data model well. |
| Containerisation | **Docker Compose** | A single `docker-compose.yml` spins up the database with environment-variable-driven credentials and a named volume for data persistence. Health checks ensure the backend only connects once the database is ready. |

---

## 3. Architecture

### 3.1 Backend Architecture

The backend follows a layered architecture organized into six separate C# projects within a single solution (`PocketAdvisor.sln`). Each project has a clear responsibility and depends only on the layers below it.

```
PocketAdvisor.WebApplication       ← HTTP entry point (controllers, middleware, DI wiring)
         │
         ▼
PocketAdvisor.Services             ← Business logic, validation, JWT generation, token management
         │
         ▼
PocketAdvisor.Repositories         ← Data access abstraction (generic CRUD over EF Core)
         │
         ▼
PocketAdvisor.DbContexts           ← EF Core DbContext, migrations, TransactionManager, DataSeeder
         │
         ▼
PocketAdvisor.Entities             ← Domain entity classes and value objects (e.g. Quantity)
         │
         ▼
PocketAdvisor.Enums                ← Shared enumerations (roles, token types, currencies, units)
PocketAdvisor.Requests             ← Inbound DTO records validated by FluentValidation
PocketAdvisor.Responses            ← Outbound DTO records returned to clients
```

**Key design decisions:**

- **Repository pattern with a generic base.** `BaseRepository<TEntity, TRepository>` implements `CreateAsync`, `ExistsAsync`, `GetSingleOrDefaultAsync`, `GetAllAsync`, `Update`, and `Delete`. Concrete repositories (e.g. `AccountRepository`) inherit from it and are registered as scoped services. This keeps data access consistent and testable.

- **Unit-of-Work via `TransactionManager`.** Database transactions are managed by `ITransactionManager`, which wraps `IDbContextTransaction`. Services call `BeginTransactionAsync`, `SaveChangesAsync` (for intermediate saves within a transaction), and `CommitTransactionAsync`. If anything throws, the manager rolls back automatically. This prevents partial writes.

- **FluentResults for business errors.** Controllers never throw for expected error conditions. Services return `Result` or `Result<T>`, and the `BaseController.HandleFailure` method inspects error metadata keys (`NotFound`, `Conflict`) to choose the right HTTP status code without duplicate branching logic.

- **Centralised exception middleware.** `ExceptionHandlingMiddleware` sits at the top of the pipeline and catches any unhandled exception, logging it and returning a structured `ProblemDetails` JSON response with status 500. In Development mode the actual exception message is included; in production a generic message is used.

- **No-tracking by default.** The `DbContext` is configured with `QueryTrackingBehavior.NoTracking` globally. Tracking is only enabled explicitly (via `asTracking: true`) in the handful of places where EF Core change tracking is needed (e.g. updating a user's password hash in place).

- **Strongly-typed configuration.** Each configuration section (JWT, token expirations, frontend URLs, SecureStore paths) is bound to a dedicated options class (e.g. `JsonWebTokenOptions`) and injected via `IOptions<T>`. Data annotations on these classes cause the application to fail fast at startup if required values are missing.

### 3.2 Frontend Architecture

The frontend is an Angular 21 application organized around feature modules with lazy loading.

```
src/app/
├── core/               ← Singleton services, guards, interceptors, models, enums
│   ├── enums/          ← Mirrors of backend enums (CurrencyCode, Unit, UnitCategory)
│   ├── guards/         ← authGuard, noAuthGuard, adminGuard (route protection)
│   ├── interceptors/   ← sessionInterceptor (JWT injection + silent token refresh)
│   ├── models/         ← Request/response TypeScript interfaces
│   └── services/       ← AccountService, CategoryService, ItemService,
│                           SessionService, TransactionService, UserService, ApiErrorService
│
├── accounts/           ← Account list, transactions page, create/update/delete dialogs
├── admin/              ← Global category management (admin-only)
├── auth/               ← Login, register, forgot-password, reset-password, verify-email
├── categories/         ← Personal category list and dialogs
├── items/              ← Item list, item detail (with Chart.js chart), dialogs
└── shared/             ← Shared components (Navbar)
```

**Key design decisions:**

- **Lazy-loaded feature modules.** Each feature area is a separate Angular module loaded on demand via `loadChildren`. This keeps the initial bundle small; the router only downloads a module's code when the user navigates to it.

- **Route guards for access control.** `authGuard` checks whether a JWT is stored before allowing access to any authenticated route. `adminGuard` additionally decodes the JWT payload client-side and checks the role claim, redirecting non-admins to the login page. `noAuthGuard` prevents already-authenticated users from seeing the auth pages.

- **Silent token refresh in the interceptor.** `sessionInterceptor` attaches the stored JWT as a `Bearer` token to every outgoing request (except the refresh endpoint itself). On receiving a 401, it automatically calls the refresh endpoint to obtain a new JWT and refresh token, then retries the original request. A shared `Observable` (`shareReplay(1)`) ensures that if multiple requests 401 simultaneously, only one refresh call is made and all callers wait for it.

- **Server-side error mapping via `ApiErrorService`.** The backend returns `ValidationProblemDetails` with a map of property names to error messages. `ApiErrorService.applyErrors` walks that map, resolves both flat keys (`"Email"`) and nested array paths (`"Items[0].ItemId"`) to the matching `AbstractControl`, and calls `setErrors({ serverError: message })` on it. This keeps all validation feedback inside the reactive form without any frontend validators.

- **Signals for component state.** Pages use `signal()` for loading flags, data arrays, and selected IDs, and `computed()` for derived values. This makes data flow explicit and avoids `ngOnChanges` boilerplate.

---

## 4. Data Model

```
User
 ├── id (PK, UUID)
 ├── email (unique)
 ├── passwordHash
 ├── isEmailVerified
 ├── role (Administrator | User)
 ├── Accounts[]
 ├── Categories[]
 ├── Items[]
 └── Tokens[]

Account
 ├── id (PK, UUID)
 ├── name (unique per user)
 ├── balance (decimal 18,2)
 ├── currencyCode (ISO 4217 numeric enum)
 └── userId (FK → User)

Category
 ├── id (PK, UUID)
 ├── name (unique per userId / globally when userId is null)
 └── userId (FK → User, nullable — null means global)

Item
 ├── id (PK, UUID)
 ├── name (unique per user)
 ├── unitCategory (Uncategorized | Length | Mass | Area | Volume | Time | Energy | DataSize)
 └── userId (FK → User)

Transaction
 ├── id (PK, UUID)
 ├── occurredAt (UTC datetime)
 ├── categoryId (FK → Category, Restrict on delete)
 ├── fromAccountId (FK → Account, nullable — null means income)
 ├── toAccountId (FK → Account, nullable — null means expense)
 └── TransactionItems[]

TransactionItem  (composite PK: transactionId + itemId)
 ├── transactionId (FK → Transaction, Cascade on delete)
 ├── itemId (FK → Item, Restrict on delete)
 ├── totalPrice (decimal 18,2)
 └── amount (Quantity value object: value decimal 18,6 + unit enum)

Token
 ├── id (PK, UUID)
 ├── hash (HMACSHA256, base-64, unique)
 ├── expiryAt (UTC datetime)
 ├── type (EmailVerification | PasswordReset | Refresh)
 └── userId (FK → User, Cascade on delete)
```

**Transaction semantics:**

| `fromAccountId` | `toAccountId` | Meaning |
|---|---|---|
| set | null | Expense (money leaves an account) |
| null | set | Income (money enters an account) |
| set | set | Transfer between two accounts |

The `Quantity` value object stored in `TransactionItem.Amount` is a fully comparable, hashable type that supports cross-unit comparison within the same physical category (e.g. grams vs kilograms) using conversion factors defined in enum extensions.

---

## 5. API Reference

All endpoints are prefixed with `/api`. Authenticated endpoints require an `Authorization: Bearer <jwt>` header. The application exposes Swagger UI at startup in all environments.

### Sessions

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/sessions/login` | None | Authenticate; returns JWT + refresh token |
| POST | `/api/sessions/refresh` | None | Rotate refresh token; returns new JWT + refresh token |

### Users

| Method | Path | Auth | Description |
|---|---|---|---|
| POST | `/api/users` | None | Register a new user; triggers verification email |
| POST | `/api/users/verify-email` | None | Verify email using a one-time token |
| POST | `/api/users/forgot-password` | None | Send a password-reset email |
| POST | `/api/users/reset-password` | None | Reset password using a one-time token |

### Accounts

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/accounts` | User | List all accounts for the current user |
| POST | `/api/accounts` | User | Create a new account |
| PATCH | `/api/accounts/{id}/name` | User | Rename an account |
| DELETE | `/api/accounts/{id}` | User | Delete an account |

### Categories

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/categories` | User | List all categories visible to the current user (personal + global) |
| POST | `/api/categories/personal` | User | Create a personal category |
| PATCH | `/api/categories/personal/{id}/name` | User | Rename a personal category |
| DELETE | `/api/categories/personal/{id}` | User | Delete a personal category |
| POST | `/api/categories/global` | Admin | Create a global category |
| PATCH | `/api/categories/global/{id}/name` | Admin | Rename a global category |
| DELETE | `/api/categories/global/{id}` | Admin | Delete a global category |

### Items

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/items` | User | List all items for the current user |
| POST | `/api/items` | User | Create a new item |
| PATCH | `/api/items/{id}/name` | User | Rename an item |
| DELETE | `/api/items/{id}` | User | Delete an item |

### Transactions

| Method | Path | Auth | Description |
|---|---|---|---|
| GET | `/api/transactions?accountId={id}` | User | List transactions for an account |
| GET | `/api/transactions?itemId={id}` | User | List transactions that include an item |
| POST | `/api/transactions` | User | Create a transaction with its items |
| DELETE | `/api/transactions/{id}` | User | Delete a transaction and all its items |
| DELETE | `/api/transactions/{transactionId}/items/{itemId}` | User | Remove a single item from a transaction (not allowed if it is the last item) |

---

## 6. Authentication & Security

### Token lifecycle

1. On **login**, the backend issues a short-lived JWT (15 minutes) and a long-lived refresh token (14 days).
2. The refresh token is stored only as an HMACSHA256 hash in the `Tokens` table. The plain value is sent to the client once and never stored server-side.
3. When the JWT expires, the Angular interceptor transparently calls `POST /api/sessions/refresh`. The old refresh token is deleted and a new one is issued (token rotation). If the refresh token is also expired or absent, the user is logged out.
4. Password reset and email verification tokens follow the same hash-only storage pattern, each with its own HMAC secret to prevent cross-type forgery.

### JWT claims

| Claim | Value |
|---|---|
| `sub` | User GUID |
| `role` | `"Administrator"` or `"User"` |
| `iss` | Configured issuer URL |
| `aud` | Configured audience URL |
| `nbf` / `exp` | Issued-at / expiry |

### Password security

Passwords are hashed with ASP.NET Core Identity's `PasswordHasher<T>`, which uses PBKDF2-SHA512 with a random salt. On successful login, if the hasher detects that the stored hash uses an older format, it rehashes the password transparently and updates the database.

### CORS

A global CORS policy is registered that allows requests only from the configured frontend base URL. All other origins are rejected.

### Secret management

Sensitive values (JWT signing key, HMAC secrets for the three token types, Resend API key) are stored in an encrypted `secrets.bin` file managed by the SecureStore library, with the decryption key in a separate `secrets.key` file. This file is not committed to source control.

---

## 7. Functional Requirements

### User management

- A visitor can register with an email address and password. Registration sends a verification email.
- A user must verify their email before they can log in.
- A user can request a password-reset email. The reset link is valid for 30 minutes.
- Authenticated sessions are maintained with a JWT (15-minute lifetime) that is silently refreshed using a rotating refresh token (14-day lifetime).

### Account management

- A user can create multiple named accounts, each with a starting balance and a currency (any ISO 4217 currency).
- Account names must be unique per user.
- A user can rename or delete any of their own accounts.

### Category management

- An administrator can create, rename, and delete global categories visible to all users.
- A user can create, rename, and delete their own personal categories.
- A user sees both global and personal categories when creating a transaction.
- Global categories cannot be deleted while they are referenced by a transaction (database-level restrict).

### Item management

- A user can create named items, each assigned to a unit category (e.g. Mass, Volume, Time).
- Item names must be unique per user.
- A user can rename or delete any of their own items.
- Items cannot be deleted while they are referenced by a transaction (database-level restrict).

### Transaction management

- A user can create a transaction by specifying:
  - The date and time it occurred.
  - A category (global or personal).
  - A source account (null for income), a destination account (null for expense), or both (transfer).
  - One or more items, each with a total price and a quantity (value + unit).
- A user can view all transactions for a given account, or all transactions that include a given item.
- A user can delete an entire transaction (including all its items).
- A user can remove a single item from a transaction, provided it is not the only item.

### Item detail / analytics

- A user can open a detail view for any item, which shows:
  - Every transaction that includes that item, sorted chronologically.
  - A line chart of spending on that item over time (aggregated by day).
  - A grand total (shown only when all transactions share the same currency).
  - The expandable list of items within each transaction.

### Admin panel

- An administrator has access to a dedicated admin section for managing global categories, inaccessible to regular users.

---

## 8. Non-Functional Requirements

### REST compliance

The API is designed to conform to REST principles:

- Resources are identified by URL nouns (`/api/accounts`, `/api/transactions/{id}`).
- HTTP methods are used semantically: `GET` for retrieval, `POST` for creation, `PATCH` for partial update, `DELETE` for removal.
- HTTP status codes are used accurately: `201 Created` for new resources, `204 No Content` for successful mutations that return nothing, `400 Bad Request` with a `ValidationProblemDetails` body for validation failures, `401 Unauthorized` for missing/invalid tokens, `403 Forbidden` for role violations, `404 Not Found` for missing resources, `409 Conflict` for constraint violations, `500 Internal Server Error` for unexpected failures.
- The API is stateless: all authentication state is carried in the JWT on each request, with no server-side session.

### Security

- Passwords are never stored in plain text.
- Sensitive tokens (refresh, email verification, password reset) are stored only as HMAC hashes; the plain values are transmitted once and immediately discarded server-side.
- All secrets (signing keys, HMAC keys, API keys) are stored in an encrypted file outside the application's configuration JSON.
- JWTs have a short 15-minute expiry to limit the window of exposure if intercepted.
- CORS restricts the browser origins that can call the API.

### Error handling

- All unhandled server exceptions are caught by middleware and returned as RFC 9457 `ProblemDetails` JSON, preventing stack trace leakage in production.
- Validation errors are returned as `ValidationProblemDetails` with a per-field error map, which the frontend maps directly onto reactive form controls.
- Business errors (not-found, conflict) are expressed as typed result metadata, not exceptions, ensuring consistent HTTP status codes without duplicated conditional logic.

### Observability

- All layers (repositories, services, middleware) emit structured log messages via `ILogger<T>` with appropriate severity levels.
- Log messages include contextual data (entity names, user IDs, transaction IDs) using structured logging syntax so they can be filtered and queried in any log aggregation system.
- The exception middleware includes the `TraceIdentifier` of the HTTP context in error log entries for correlation.

### Scalability considerations

- The no-tracking default for EF Core queries avoids unnecessary object graph materialization, reducing memory pressure under load.
- Lazy-loaded frontend modules keep the initial JavaScript payload small, improving time-to-interactive for first-time visitors.
- Chart.js is loaded from a CDN rather than bundled, further reducing the application bundle size.

### Developer experience

- Automatic EF Core migrations are applied on startup, so the database schema is always in sync with the code after a deployment.
- In Development mode, a `DataSeeder` populates the database with realistic test data (two users, multiple accounts, categories, items, and 32 transactions) if no data exists, enabling immediate exploration without manual setup.
- Swagger UI is available in all environments for API exploration.
- Prettier enforces consistent code formatting across the frontend without developer configuration.
- Central package version management (`Directory.Packages.props`) ensures all backend projects use identical dependency versions.
