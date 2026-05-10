# AI Assistance Summary — PocketAdvisor

This document summarizes which parts of the PocketAdvisor project were built with AI assistance, highlighting examples of where the AI performed well and where it fell short.

---

## Areas of AI Assistance

### Backend

| Area | Files / Features |
|---|---|
| Base infrastructure | `BaseController` validation helper, `BaseService`, `BaseRepository` generics |
| Auth | JWT configuration, Swagger JWT security definition, token claim extraction, refresh endpoint |
| Account endpoints | `POST /api/accounts`, `DELETE /api/accounts/{id}`, `PATCH /api/accounts/{id}/name`, `GET /api/accounts` |
| Category endpoints | `POST /api/categories/personal`, `POST /api/categories/global`, `PATCH` name routes, `DELETE` routes with restrict-constraint handling |
| Transaction endpoints | Create, delete, get (with `TransactionItem` includes), transfer logic |
| Item endpoints | Create, delete, update name |
| Data seeder | 2 users, 8 global categories, 25 transactions |
| Secret management | `SecretManager` using `NeoSmart.SecureStore` |
| Exception middleware | Global error handling pipeline |
| Email verification & password reset | Full endpoint + token flow |

### Frontend

| Area | Files / Features |
|---|---|
| Services & models | All TypeScript enums, 10+ interfaces, 6 focused services (`AccountService`, `CategoryService`, `ItemService`, `TransactionService`, `SessionService`, `UserService`), `SessionInterceptor` |
| Route guards | `authGuard`, `noAuthGuard`, `adminGuard` (JWT role decode) |
| Angular Material theme | Full M3 `material-theme.scss` with 6 custom palettes and all tonal stops |
| Auth module | Login, Register, ForgotPassword pages, `AuthCardComponent`, `ApiErrorService` |
| Accounts module | Account list, create dialog, delete dialog, rename dialog, account transactions page, create transaction dialog (FormArray, transfer support, nested `ApiErrorService` path parsing) |
| Categories module | Personal + global category list with create/rename/delete dialogs |
| Admin module | `adminGuard`, global category management page |
| Navbar | Routing integration, `isLoggedIn` signal fix |

---

## Examples Where AI Did Well

### Full account endpoint stack — first attempt (`account-controller.md`)

The AI read the entire codebase before writing a single line, finding the existing empty `IAccountRepository`, the validator discovery pattern, `ValidationMessages.resx` + `Designer.cs`, and the `BaseController`/`BaseService`/`BaseRepository` generics. It then produced a complete, build-passing stack in one turn: `CreateAccountRequest`, `CreateAccountRequestValidator`, service interface and implementation, DI registration, and `AccountController`. All design choices were correct on first attempt — `[Authorize]` at controller level, `201 Created` response, service receiving `userId` as a parameter to keep HTTP concerns out of the service layer.

### Complete frontend service layer — single session (`angular-services.md`)

In one session the AI generated all TypeScript enums (correctly mirroring the C# enums), all request/response model interfaces, six focused services matching the controller split, `SessionService` with `localStorage` and `tap` side effects, and a working `HttpInterceptorFn` interceptor — all wired into `app.config.ts`. It proactively suggested splitting the service layer by controller (rather than a monolithic `ApiService`) without being asked.

### Pre-planned constraint discovery — category delete (`category-delete-endpoints.md`)

Before writing any code, the AI found the `DeleteBehavior.Restrict` constraint on the `Transaction → Category` foreign key in `PocketAdvisorDbContext`, correctly predicted that a raw delete would throw at the database level, and designed the 400 "category has transactions" business rule accordingly. Service and controller were correct on first submission.

### Full categories module — single turn (`categories-page-component.md`)

The AI mirrored the accounts page pattern exactly — same signal structure, same dialog open/close/refresh cycle — and built `CategoryList` plus all three dialogs (create, rename, delete) in one response. No functional issues were reported.

### Runtime error diagnosis

When given error messages, the AI diagnosed root causes accurately and quickly on several occasions: `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap` remapping `sub` to a WS-Federation URI, `DeleteBehavior.Restrict` throwing on category deletion, and a `System.MissingMethodException` caused by a `Microsoft.OpenApi` version conflict with Swashbuckle.

---

## Examples Where AI Struggled

### Angular Material M3 palette nesting — 5+ turns (`auth-module.md`, `angular-material-custom-theme.md`)

The AI did not know that Angular Material M3's `mat.theme()` requires sub-palettes (`error`, `neutral`, `neutral-variant`) to be nested inside the primary palette map rather than passed as separate top-level keys. This caused all tonal stop CSS variables to render as blank `light-dark(,)` values. Diagnosing the issue required the user to copy the computed DOM variables across multiple turns. It took five or more turns to fully resolve all missing stops — the longest single debugging sequence in the project.

### Frontend `Validators` added despite project convention — multiple files

The project convention is backend-only validation: no `Validators` on reactive form controls, errors mapped exclusively via `ApiErrorService`. Despite this, the AI added `Validators.required` and `Validators.maxLength` in both `CreateAccountDialog` (Turn 2) and `CreateTransactionDialog` (Turn 4) and had to be told to remove them each time. This happened often enough that it was saved as a persistent memory rule, yet the AI still repeated the mistake on a new component.

### Category creation business logic — two bugs on first attempt (`category-creation-endpoints.md`)

Two logic errors in a single turn:
1. `CreatePersonalCategoryAsync` only checked for duplicate personal category names for that user — it missed the case where a global category with the same name already existed.
2. `CreateGlobalCategoryAsync` did not implement the absorption/promotion logic (re-pointing existing personal category transactions to the new global category, then deleting the personal categories). The user had to point both out explicitly before a corrected version was produced.

### Category name update — two separate omissions (`category-name-update-endpoints.md`)

Two separate turns, two separate oversights:
1. The personal update route was `PATCH /api/categories/{id}/name` — missing the `/personal` prefix, inconsistent with all other personal category routes.
2. `UpdateGlobalCategoryNameAsync` was missing the personal-category consolidation block that `CreateGlobalCategoryAsync` already had. The absorption logic was not carried over when renaming a global category to a name held by personal categories.

### Transaction dialog item row layout — 7 turns (`account-transactions-create-dialog.md`)

The CSS grid layout for the item rows inside the create transaction dialog required seven consecutive correction turns: overflow clipping, 2-row vs. 1-row grid, column ratio attempts (3fr/2fr, 2fr/3fr, 1fr/2fr), gap sizing, and column order. The AI never converged on the right layout from a description alone and required repeated manual correction to reach the final result.

### Missing `formArrayName` attribute (`account-transactions-create-dialog.md`)

The items `FormArray` template was missing `formArrayName="items"` on the container element. Angular threw `Cannot find control with name: '0'` at runtime. A basic reactive forms requirement that was overlooked.

### `auth.css` not persisted to disk (`auth-module.md`)

The AI created `auth.css` in Turn 3 and added an `@import` in `styles.css`. By Turn 6 it turned out the file was never actually saved to disk during the session. All styles had ended up inside `auth-card.css` instead, and Angular's `ViewEncapsulation.Emulated` meant projected form content could not see those styles. The approach had to be redesigned.

### Amount display spacing (`account-transactions-page.md`)

The amount sign and value were rendered as two separate interpolation expressions — `{{sign}}{{amount | currency}}` — causing Angular to insert a space between them, producing `+ $50.00` instead of `+$50.00`. Required merging into a single string expression.

---

## Patterns & Observations

**Backend was more reliable than frontend.** The AI consistently followed the layered architecture, matched existing naming conventions, and produced build-passing code. When backend mistakes occurred they were logical or business-rule oversights — not structural problems. The frontend had more iteration, particularly around Angular Material internals, CSS layout, and reactive forms conventions.

**REST endpoints were cleaner than UI components.** The AI read all relevant files, planned the full stack (request → validator → service → controller), and the code was immediately functional in most cases. UI components required more back-and-forth, especially around Angular Material-specific behavior that the AI either misunderstood or was simply not aware of.

**Recurring mistakes:**
- Adding frontend `Validators` despite the established convention (happened at least twice).
- Misunderstanding Angular Material M3 internals (palette nesting, `ViewEncapsulation`, CSS token names).
- Incomplete business logic for category operations on first attempt — the global/personal symmetry and absorption rules were complex enough to trip the AI up in both the Create and Update paths.

**Where the AI added the most value:**
- Large-scale scaffolding: generating entire module stacks (request, validator, service, controller, resource file entries) in one turn while correctly following established patterns.
- Proactive codebase reading before writing — the AI almost always read all relevant existing files first, which meant conventions were matched and existing patterns were reused.
- Debugging runtime errors given an error message — JWT claim remapping, version conflicts, EF Core constraint exceptions were all diagnosed accurately.
- Design rationale — the exported conversation files contain well-reasoned explanations for structural decisions that would otherwise not be documented.
