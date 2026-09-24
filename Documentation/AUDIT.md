# PocketAdvisor — Security & Architecture Audit

**Date:** 2026-09-21

This document records the findings from the initial security and architecture review conducted
before the hardening sprint. Issues are grouped by severity and area.

---

## Critical

### JWT + refresh token stored in `localStorage`

**File:** `Frontend/src/app/core/services/session.service.ts`

Both the JWT access token and the refresh token are persisted in `localStorage`.
`localStorage` is readable by any JavaScript running on the same origin, making both tokens
trivially exfiltrable via an XSS payload. The risk is compounded by the fact that a stolen
refresh token is valid for 14 days.

**Recommendation:** Store the refresh token in an `HttpOnly; Secure; SameSite=Strict` cookie
so JavaScript cannot read it. The access token can remain in memory (a reactive signal) and be
refreshed silently on page load via the existing `SessionInterceptor` refresh flow.

---

### `secrets.bin` committed to the repository

**File:** `Backend/PocketAdvisor.WebApplication/secrets.bin` (and `secrets.key`)

The encrypted secret store file is tracked by git. Even though the file is encrypted,
committing it means the ciphertext (and the corresponding key file) travel with the
repository history permanently.

**Recommendation:** Add both `secrets.bin` and `secrets.key` to `.gitignore`, remove them
from tracking (`git rm --cached`), and document a setup script that regenerates them from
environment variables on a clean clone.

---

## High

### User enumeration in `ForgotPasswordAsync`

**File:** `Backend/PocketAdvisor.Domain/Services/UserService.cs`

When a non-existent email is submitted to `POST /api/users/forgot-password`, the endpoint
returns a distinct error (user not found). An attacker can use this to enumerate valid
email addresses by observing whether the response is a 404 or a success.

**Recommendation:** Always return `200 OK` with a generic "if that email exists, a reset
link has been sent" message, regardless of whether the user was found.

---

### Email sending in controller layer

**File:** `Backend/PocketAdvisor.WebApplication/Controllers/UserController.cs`

`UserController` calls the Resend email API directly. Infrastructure concerns (sending email)
belong in the service layer or a dedicated infrastructure service, not in the presentation
layer. This violates the layered architecture used everywhere else in the project.

**Recommendation:** Move email dispatch into `UserService` (or a dedicated `IEmailService`),
keeping the controller responsible only for HTTP concerns.

---

## Medium

### `GetAccountsAsync` loads all transactions into memory

**File:** `Backend/PocketAdvisor.Domain/Services/AccountService.cs`

Balance calculation iterates over all transactions fetched into memory. For accounts with
many transactions this causes unbounded memory usage and slow queries.

**Recommendation:** Replace the in-memory summation with a SQL aggregation:
`SUM(CASE WHEN type = 'Credit' THEN amount ELSE -amount END)` via a raw query or
EF Core's `GroupBy` + `Sum`.

---

### `Categories` unique index allows duplicate global category names

**File:** EF Core migration / `PocketAdvisorDbContext`

The unique index on `(Name, UserId)` uses standard `NULL` semantics: two rows with
`UserId IS NULL` (global categories) are considered distinct, so duplicate global category
names are possible at the database level.

**Recommendation:** Add `NULLS NOT DISTINCT` to the index definition (PostgreSQL 15+):

```sql
CREATE UNIQUE INDEX ix_categories_name_userid
ON "Categories" ("Name", "UserId") NULLS NOT DISTINCT;
```

---

### `BaseService` uses the service locator antipattern

**File:** `Backend/PocketAdvisor.Domain/Services/BaseService.cs`

`BaseService` takes `IServiceProvider` and resolves dependencies by calling
`GetRequiredService<T>()`. This hides dependencies, makes unit testing harder, and bypasses
the DI container's lifetime validation.

**Recommendation:** Replace `IServiceProvider` with explicit constructor parameters for
each dependency.

---

### Cross-currency transfers not validated

**File:** Transaction creation logic

The transfer flow allows a transaction to move funds between accounts with different
currencies without any validation or conversion. This silently produces incorrect balances.

**Recommendation:** Either reject cross-currency transfers with a `400 Bad Request`, or
implement a conversion step using a stored or fetched exchange rate.

---

### No "resend verification email" endpoint

A user who loses the verification email or whose token expires has no way to request a
new one. The registration flow is a dead end in that scenario.

**Recommendation:** Add `POST /api/users/resend-verification` that accepts the email
address and issues a new token, with rate limiting to prevent abuse.

---

## Low

### No production environment file in Angular

**File:** `Frontend/src/environments/`

`environment.ts` exists but `environment.prod.ts` does not. The API base URL is
hardcoded to `http://localhost:5078/api`, so a production build silently targets localhost.

**Recommendation:** Add `environment.prod.ts` with the production API URL and reference it
in the Angular build configuration under `fileReplacements`.

---

### SSR remnants in `package.json`

**File:** `Frontend/package.json`

A `serve:ssr` script is present, but `@angular/ssr` is not listed as a dependency and no
SSR entry point exists. The script will always fail.

**Recommendation:** Remove the `serve:ssr` script, or add `@angular/ssr` and configure it
properly if SSR is actually intended.

---

### Zero automated tests

No `.spec.ts` files exist in the Angular project and no xUnit/NUnit test project exists
in the Backend solution. There is no CI configuration (`.github/workflows/`).

**Recommendation:** Add at minimum integration tests for the authentication and account
endpoints (WebApplicationFactory), and component tests for the session service. Set up
a GitHub Actions workflow that runs tests on every pull request.
