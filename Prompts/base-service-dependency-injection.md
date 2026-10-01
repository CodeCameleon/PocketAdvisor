# Base Service Dependency Injection Refactor

**Date:** 2026-09-29

---

## User Prompt 1

> I want you to fix the base service service locator aduit fining on a new feature branch.

---

## Assistant Response 1

Investigated the audit finding first, then implemented the fix on a new branch.

- Read `Documentation/AUDIT.md` to find the exact finding ("`BaseService` uses the
  service locator antipattern")
- Created branch `feature/base-service-dependency-injection` from `origin/dev` (the
  integration branch the previous feature PR merged into)
- Replaced `IServiceProvider` in `BaseService<TService>` with explicit constructor
  parameters
- Removed the `GetValidator<TRequest>()` service-locator helper entirely
- Gave each of the five services (`AccountService`, `CategoryService`, `ItemService`,
  `TransactionService`, `UserService`) an explicit `IValidator<TRequest>` constructor
  parameter for every validator it used, each with its own null check, backing property,
  and XML doc comment
- Replaced `TransactionManager.Value.X()` calls with direct `TransactionManager.X()`
  calls, since the transaction manager was also resolved through `IServiceProvider` via a
  `Lazy<>` wrapper

In `BaseService.cs`:
- Constructor changed from `BaseService(ILogger<TService> logger, IServiceProvider serviceProvider)`
  to `BaseService(ILogger<TService> logger, ITransactionManager transactionManager)`
- `TransactionManager` property changed from `Lazy<ITransactionManager>` to
  `ITransactionManager`, assigned directly in the constructor
- Removed the `ServiceProvider` property and the `GetValidator<TRequest>()` region entirely

In each service (`AccountService.cs`, `CategoryService.cs`, `ItemService.cs`,
`TransactionService.cs`, `UserService.cs`):
- Constructor signature changed to take `ITransactionManager transactionManager` plus one
  `IValidator<TRequest>` parameter per request type the service validates
- Added `ArgumentNullException.ThrowIfNull(...)` for each new parameter
- Added a private `IValidator<TRequest>` property per validator, with an XML doc comment
- Replaced every `IValidator<TRequest> validator = GetValidator<TRequest>(); ... await
  validator.ValidateAsync(request)` pair with a single
  `await XxxRequestValidator.ValidateAsync(request)` call

Hit a tooling snag along the way: git left a stale, empty `.git/index.lock` behind partway
through the branch switch (the folder blocks deletions by default), which stopped `git
status`/`git switch` from completing. Asked for delete permission on the connected folder,
removed the lock, confirmed the working tree was clean on the original branch before
re-creating and switching to the new branch cleanly.

Could not run `dotnet build` to verify the result — the .NET SDK isn't installed in either
the linked computer's workspace or the cloud workspace, and downloading it (`dot.net`,
`builds.dotnet.microsoft.com`) and NuGet (`api.nuget.org`) were both blocked by the network
allowlist in both environments. Reviewed the diff by hand instead (parameter counts, call
sites, `using` directives, line lengths) and flagged that `dotnet build` still needed to be
run before committing.

Also noted two related findings while in the area, left untouched as out of scope:
- `BaseController.BadRequest` has the same service-locator pattern
  (`HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>()`), fixable with
  `ControllerBase`'s own `ProblemDetailsFactory` property
- The rate limiter's static `OnRejectedAsync` callback and the seeding scope in `Program.cs`
  also resolve services directly, but both are standard practice in their contexts (a static
  callback can't take constructor parameters; `Program.cs` is where services get wired up)

---

## User Prompt 2

> The problem I see is that now all services will you the transaction manager, that was the
> reason it was in Lazy. If you want to keep the current change, then create a new base
> service that specializes the current one for working with a database.

---

## Assistant Response 2

Good catch — confirmed all five services actually call `TransactionManager.X()` at least
once, so the `Lazy<>` wasn't incidental: it existed so a hypothetical non-database service
wouldn't force a `TransactionManager` (and its `DbContext`) to be constructed. Split the base
class in two instead of just removing the laziness:

- `BaseService<TService>` reduced back to just the logger and the error-creation helpers
  (`CreateError`, `CreateNotFoundError`, `CreateConflictError`) — no transaction manager, no
  database dependency at all
- New `BaseDatabaseService<TService>` added, inheriting from `BaseService<TService>` and
  adding the `ITransactionManager` constructor parameter and property
- All five services switched from `: BaseService<X>, IXService` to
  `: BaseDatabaseService<X>, IXService`, since all five use the transaction manager

This keeps the fix (no more `IServiceProvider`/service locator) while preserving the original
intent behind the laziness: a future service that doesn't touch the database can inherit from
`BaseService` directly and never pay for a `TransactionManager`/`DbContext` it doesn't need.
At the moment every service happens to be a database service, so this changes nothing at
runtime yet — it only matters once a non-database service is added.

Followed the project's existing region/doc-comment conventions for the new
`BaseDatabaseService.cs` file (XML docs, `#region Constructors` / `#region Properties`,
`ArgumentNullException.ThrowIfNull`, LF line endings, no BOM).

Hit the same stale `.git/index.lock` issue again after editing; asked for delete permission
again and cleared it.

Still could not run `dotnet build` — same network restrictions as before.

---

## User Prompt 3

> I made some changes to the code. Please review it.

---

## Assistant Response 3

Diffed the user's own commit (`36c69b0`, "Implemented the base database service class.")
against `origin/dev` and against what had been left in Response 2, rather than assuming
nothing had changed.

Found the commit matched the intended design exactly — `BaseService` → logger only,
`BaseDatabaseService` → adds the transaction manager, all five services inheriting from
`BaseDatabaseService` — with two cosmetic differences from the draft:
- Validator XML docs reworded from "The validator for the `<see cref="X" />` request model."
  to "The validator for the `<see cref="X" />` model." (consistent across all five services)
- Long `<param>` doc comments wrapped onto their own lines (matching the project's existing
  convention), short ones kept inline

Checked for correctness issues: no `IServiceProvider` or `GetValidator` left anywhere, no
line over 120 characters, `using` directives still alphabetical, null checks present for
every new parameter, single commit with a properly-tense message, clean working tree.

Flagged three minor, non-blocking points:
1. Property names (e.g. `CreateAccountRequestValidator`) match the validator *class* names
   from `PocketAdvisor.Requests.*`, which is unambiguous to the compiler (property lookup
   wins) but could visually read as a static call — optional rename suggested if it bothers
   the user
2. `BaseService.cs` and `UserService.cs` still have a UTF-8 BOM, inconsistent with
   `.editorconfig` and the repo's own "Removed the BOM encoding" commit history —
   pre-existing, not introduced by this change, worth its own small commit
3. Two related items still open: `Documentation/AUDIT.md` doesn't mark this finding resolved
   yet, and `BaseController.BadRequest` still has the analogous service-locator call

Still unable to run `dotnet build` in either environment, so recommended running it before
opening a PR.

---

## User Prompt 4

> Then please delete the audit part for this problem and fix the ProblemDetailsFactory
> problem too.

---

## Assistant Response 4

- Removed the entire "`BaseService` uses the service locator antipattern" section from
  `Documentation/AUDIT.md` (heading, file reference, description, recommendation, and the
  trailing `---` separator), leaving the Medium section flowing directly from the
  `Categories` unique-index finding to "Cross-currency transfers not validated"
- Fixed `BaseController.BadRequest` in
  `Backend/PocketAdvisor.WebApplication/Controllers/BaseController.cs`:
  - Removed `ProblemDetailsFactory problemDetailsFactory = HttpContext.RequestServices
    .GetRequiredService<ProblemDetailsFactory>();`
  - Changed `problemDetailsFactory.CreateValidationProblemDetails(...)` to
    `ProblemDetailsFactory.CreateValidationProblemDetails(...)`, using `ControllerBase`'s own
    built-in property instead of resolving it from `HttpContext.RequestServices`
  - Removed the now-unused `using Microsoft.AspNetCore.Mvc.Infrastructure;` import, since
    nothing else in the file referenced that namespace
- Verified no other `GetRequiredService`/`RequestServices` lookups remained in
  `BaseController.cs`

Confirmed the two remaining direct-resolution call sites elsewhere in the backend
(`OnRejectedAsync`'s static rate-limiter callback, and the seeding block in `Program.cs`) are
unrelated to this finding and acceptable as-is for the reasons given in Response 1.

Hit the stale `.git/index.lock` problem a third time; asked for delete permission again and
cleared it so the next commit wouldn't be blocked.

Still unverified by an actual build — flagged that `dotnet build` should be run before
committing.
