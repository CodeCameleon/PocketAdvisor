# Login Implementation

**Date:** 2026-04-20

---

## 1 · Create the Login request model

**Adam:**
> Create the Login request modal. You can find CreateUserRequest and use it as an example for code style and comments.

**Assistant (thinking):**
Read `CreateUserRequest.cs`, `CreateUserRequestValidator.cs`, `ValidationMessages.resx`, and
`ValidationMessages.Designer.cs` to understand the existing code style, comment conventions, and validation
patterns before writing anything.

`LoginRequest` needs only `Email` and `Password` — no `ConfirmPassword`, since that is registration-only.
`LoginRequestValidator` should have no repository dependency at this stage; login validation is purely format
and presence checks. Verifying whether credentials are actually correct is the service layer's job. The password
rules are intentionally lighter than `CreateUserRequestValidator`: `NotEmpty` + `MaximumLength` only — no strength
check, since revealing strength failures on login would be a security leak. All validation messages reuse existing
entries from `ValidationMessages.resx`.

**Files created:**
- `PocketAdvisor.Requests/Users/LoginRequest.cs`
- `PocketAdvisor.Requests/Users/LoginRequestValidator.cs`

---

## 2 · Add email existence check to the validator

**Adam:**
> I want you to also add the user existing check to the validator.

**Assistant (thinking):**
Adding an email existence check to the login validator has a security implication: it allows an attacker to
enumerate valid accounts by submitting emails and reading validation errors. The standard practice is to keep
login validation dumb (format only) and have the service return a generic "invalid credentials" message regardless
of whether the email or password was wrong. Flagged this tradeoff and asked Adam to confirm.

**Adam confirmed:** proceed.

**Assistant (thinking):**
Added both `ExistByEmail` and `MatchPasswordHash` async rules. `MatchPasswordHash` uses the overload that receives
the full `LoginRequest` instance so it can look up the user by email before verifying the hash.
Injected `IUserRepository` and `IPasswordHasher<User>`. Added `EmailNotFound` and `PasswordIncorrect` to both
`ValidationMessages.resx` and `ValidationMessages.Designer.cs` in the Requests project.

**Files modified:**
- `PocketAdvisor.Requests/Users/LoginRequestValidator.cs` — added repository + hasher, two new async rules
- `PocketAdvisor.Requests/Resources/ValidationMessages.resx` — added `EmailNotFound`, `PasswordIncorrect`
- `PocketAdvisor.Requests/Resources/ValidationMessages.Designer.cs` — added matching properties

---

## 3 · Revert repository checks back to service layer

**Adam:**
> You are right about the rest of the validation being in the service layer. Can you make anything that is repository
> dependent there from the Login request?

**Assistant (thinking):**
Strip `LoginRequestValidator` back to pure format checks (the original design). Remove `IUserRepository` and
`IPasswordHasher<User>` dependencies entirely. Remove `ExistByEmail` and `MatchPasswordHash` methods. Remove the
`EmailNotFound` and `PasswordIncorrect` messages from both resource files — they have no business being in the
requests layer.

**Files modified:**
- `PocketAdvisor.Requests/Users/LoginRequestValidator.cs` — reverted to format-only, no dependencies
- `PocketAdvisor.Requests/Resources/ValidationMessages.resx` — removed `EmailNotFound`, `PasswordIncorrect`
- `PocketAdvisor.Requests/Resources/ValidationMessages.Designer.cs` — removed matching properties

---

## 4 · Implement the login service method and controller action

**Adam:**
> Now implement the login service method and controller action. Be sure to follow the standard REST guidelines
> when creating the endpoint, because it is a university project. You can use the create user endpoint and
> service method for code and comments styles.

**Assistant (thinking):**
Read `UserController.cs`, `UserService.cs`, `IUserService.cs`, `BaseService.cs`, `BaseController.cs`,
`ValidationMessages.Designer.cs` (Services), `TokenExpirationsOptions.cs`, `TokenSecretsOptions.cs`, and the empty
Responses project to understand the full picture.

Key design decisions:

- **`POST /api/sessions`** rather than `POST /api/users/login`. A login creates a session — that is the resource
being created. `/api/users/login` is RPC-style and violates REST's resource-oriented constraint. A new
`SessionController` keeps concerns cleanly separated from `UserController`.
- **`LoginResponse`** DTO with `JsonWebToken` and `RefreshToken` properties, placed in the empty
`PocketAdvisor.Responses` project.
- **Single `InvalidCredentials` message** for both "user not found" and "wrong password" — the service intentionally
returns the same error regardless of which check failed, so the response gives no information
about whether the email exists.
- **`201 Created`** with the response body. A session is a new resource being created, so `201` is semantically
correct over `200`.
- **JWT left as `string.Empty` with a TODO** — the infrastructure (`TokenSecretsOptions.JsonWeb`,
`TokenExpirationsOptions.JsonWebMinutes`) is already in place, but JWT signing is a separate piece of work.
- The service fetches the user, verifies the password hash, begins a transaction, generates and persists a
refresh token, then commits.

**Files created:**
- `PocketAdvisor.Responses/Users/LoginResponse.cs`
- `PocketAdvisor.WebApplication/Controllers/SessionController.cs`

**Files modified:**
- `PocketAdvisor.Services/Interfaces/IUserService.cs` — added `LoginAsync` signature
- `PocketAdvisor.Services/Implementations/UserService.cs` — added `LoginAsync` implementation
- `PocketAdvisor.Services/Resources/ValidationMessages.resx` — added `InvalidCredentials`
- `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs` — added matching property

---

## 5 · Handle PasswordVerificationResult.SuccessRehashNeeded

**Adam:**
> Can you add a part into the login service method where the SuccessRehashNeeded is handled?

**Assistant (thinking):**
The rehash needs to happen inside the transaction alongside the refresh token creation, since it is a write to the
user entity. At the time, the user was fetched without tracking, so the plan was to re-fetch with `asTracking: true`
before calling `Update`. Added a `SuccessRehashNeeded` block inside `BeginTransactionAsync`/`CommitTransactionAsync`
that re-fetches the tracked user, recomputes the hash, calls `UserRepository.Update`, and saves before continuing to
the refresh token creation.

**Files modified:**
- `PocketAdvisor.Services/Implementations/UserService.cs` — added `SuccessRehashNeeded` block inside the transaction

---

## 6 · Fetch user as tracking from the start

**Adam:**
> Wouldn't it be better to always get the user as tracking in the first request to the database?

**Assistant (thinking):**
Yes — since a write always follows the initial fetch (either the rehash or the refresh token creation),
there is no reason to fetch without tracking and then re-fetch. Changed the initial `GetSingleOrDefaultAsync`
call to pass `asTracking: true`. The `SuccessRehashNeeded` block then simplifies from a second database round-trip
with a null guard down to four lines: recompute hash, call `Update`, `SaveChangesAsync`, log.

**Files modified:**
- `PocketAdvisor.Services/Implementations/UserService.cs` — initial fetch changed to `asTracking: true`,
rehash block simplified

---

## 7 · Add JWT Issuer and Audience to configuration

**Adam:**
> Next, I want you to add the JWT Issuer and Audience values to the appsettings.json and make them available
> in the DI with Configuration model binding. Like the TokenExpirations.

**Assistant (thinking):**
Following the exact same pattern as `TokenExpirationsOptions`: create a sealed class implementing `IBaseOptions`,
set `SectionName`, decorate required properties with `[Required]`, register via the existing `AddOptions<T>` helper
in `ServiceCollectionExtensions`. Add the section to `appsettings.json`. The existing section name in the files
was `"JsonWebToken"` (not `"Jwt"`), so the class and section name were aligned to `JsonWebTokenOptions` /
`"JsonWebToken"` accordingly. The file was also renamed from `JwtOptions.cs` to `JsonWebTokenOptions.cs` to match.

**Files created:**
- `PocketAdvisor.Services/Configurations/JsonWebTokenOptions.cs`

**Files modified:**
- `PocketAdvisor.WebApplication/appsettings.json` — added `"JsonWebToken"` section
- `PocketAdvisor.Services/Extensions/ServiceCollectionExtensions.cs` — registered `JsonWebTokenOptions`

---

## 8 · Set correct Issuer and Audience values per environment

**Adam:**
> Do you think the Issuer and Audience values are correct for both the production and the development environments?

**Assistant:**
Not really. Using the same placeholder in all environments is fine for a university project, but the more
significant issue is that these values were hardcoded as `"PocketAdvisor"` in a single `appsettings.json`.
In a real setup, development should use localhost URLs (some JWT libraries validate these as URIs) and
production should use the real domain. The right approach is `appsettings.Development.json` overriding
`appsettings.json`. Asked Adam for the backend subdomain preference and dev port.

**Adam:**
> Yes please. I own the domain "codecameleon.com" and want to use it as "pocketadvisor.codecameleon.com" for
> the frontend and something similar for the backend. Also, the web application might be used with HTTP
> sometimes, not just HTTPS.

**Adam:**
> The dev is http://localhost:5078. I don't want to go with subpath in production.

**Assistant (thinking):**
Confirmed `http://localhost:5078` against `launchSettings.json`. The issuer is the backend (the party signing
the token) and the audience is the frontend (the intended recipient).

- **Production issuer:** `https://api.pocketadvisor.codecameleon.com`
- **Production audience:** `https://pocketadvisor.codecameleon.com`
- **Dev issuer:** `http://localhost:5078`
- **Dev audience:** `http://localhost:5078` (frontend URL not yet known; update once the frontend dev port is set)

Also cleaned up the stale `"JsonWebToken"` section that existed in `appsettings.Development.json` from before,
and renamed `JwtOptions.cs` → `JsonWebTokenOptions.cs` to match the class name the user had already updated in
`ServiceCollectionExtensions.cs`.

Note on the dev audience: once the frontend runs on its own port (e.g. `http://localhost:3000`), that value
should be updated in `appsettings.Development.json`.

**Files modified:**
- `PocketAdvisor.WebApplication/appsettings.json` — production Issuer + Audience URLs
- `PocketAdvisor.WebApplication/appsettings.Development.json` — localhost override, stale section cleaned up
- `PocketAdvisor.Services/Configurations/JsonWebTokenOptions.cs` — class renamed, `SectionName` corrected

---

## Files touched across this session

| File                                                               | Change                                                  |
|--------------------------------------------------------------------|---------------------------------------------------------|
| `PocketAdvisor.Requests/Users/LoginRequest.cs`                     | Created                                                 |
| `PocketAdvisor.Requests/Users/LoginRequestValidator.cs`            | Created, then revised twice                             |
| `PocketAdvisor.Requests/Resources/ValidationMessages.resx`         | Added then removed `EmailNotFound`, `PasswordIncorrect` |
| `PocketAdvisor.Requests/Resources/ValidationMessages.Designer.cs`  | Added then removed matching properties                  |
| `PocketAdvisor.Responses/Users/LoginResponse.cs`                   | Created                                                 |
| `PocketAdvisor.WebApplication/Controllers/SessionController.cs`    | Created                                                 |
| `PocketAdvisor.Services/Interfaces/IUserService.cs`                | Added `LoginAsync`                                      |
| `PocketAdvisor.Services/Implementations/UserService.cs`            | Added `LoginAsync`, rehash handling, tracking fix       |
| `PocketAdvisor.Services/Resources/ValidationMessages.resx`         | Added `InvalidCredentials`                              |
| `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`  | Added matching property                                 |
| `PocketAdvisor.Services/Configurations/JsonWebTokenOptions.cs`     | Created (renamed from `JwtOptions.cs`)                  |
| `PocketAdvisor.Services/Extensions/ServiceCollectionExtensions.cs` | Registered `JsonWebTokenOptions`                        |
| `PocketAdvisor.WebApplication/appsettings.json`                    | Added `JsonWebToken` section with production URLs       |
| `PocketAdvisor.WebApplication/appsettings.Development.json`        | Added `JsonWebToken` override with localhost URLs       |
