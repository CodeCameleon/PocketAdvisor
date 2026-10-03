# Email Service Layer Refactor

**Date:** 2026-10-01

---

## User Prompt 1

> I want you to fix the email service audit finding on a new feature branch.

---

## Assistant Response 1

Investigated the audit finding first, then implemented the fix on a new branch.

- Read `Documentation/AUDIT.md` to find the exact finding ("Email sending in controller
  layer"): `UserController` called the Resend API directly, putting an infrastructure
  concern in the presentation layer
- Created branch `feature/email-service` from `feature/base-service-dependency-injection`
  rather than `dev`, because that branch (not yet merged at the time) had already replaced
  the service locator in `UserService` with constructor injection; branching from `dev` would
  have meant either reintroducing the antipattern or a guaranteed merge conflict
- Added `IEmailService` / `EmailService` in `PocketAdvisor.Services`, holding everything that
  used to live in the controller: the Resend client, the two template IDs, the template
  variable names and the frontend link building (`{BaseUrl}{Path}?token={token}`)
- Injected the email service into `UserService` and moved the dispatch there, after the
  token is committed
- Changed `IUserService.CreateUserAsync` and `ForgotPasswordAsync` from `Result<string>` to
  `Result`, so the plain one-time tokens never leave the service layer
- Reduced `UserController` to HTTP concerns only: removed the Resend client, both options
  classes and all email constants; the constructor became the one-line form used by the other
  controllers. The API contract (201 / 204) stayed the same, so the frontend needed no change
- Added the `Resend` package reference to `PocketAdvisor.Services.csproj`
- Removed the finding from `AUDIT.md` (same approach as the previous branch) and added an
  "Infrastructure behind service interfaces" note to the backend architecture section of
  `DOCUMENTATION.md`

Checked the Resend 0.19.0 assembly metadata before relying on it: `EmailMessageTemplate`
exposes `TemplateId` as `string` and `Variables` as `Dictionary<string, object>?`, so the new
helper method's `Dictionary<string, object>` parameter matches.

Hit the stale `.git/index.lock` problem right after creating the branch (the connected folder
blocks deletions by default); asked for delete permission and removed it.

Could not run `dotnet build` — the .NET SDK isn't installed on the linked computer's shell,
and NuGet / the .NET download hosts are blocked in the cloud workspace. Reviewed the diff by
hand and flagged that a build was still needed before committing.

Pointed out two pre-existing issues left out of scope:
- If Resend fails after the commit, the user / token is already saved but the client gets a
  500 (same as before the change)
- The user enumeration finding now lives in `UserService.ForgotPasswordAsync`

---

## User Prompt 2

> Isn't the service in a service an antipattern?

---

## Assistant Response 2

Explained that injecting one class into another is not the antipattern in itself; the
antipattern is peer *business* services depending on each other (hidden coupling, tangled
transaction boundaries, possible cycles). Business services should only depend on things
below them — repositories, the transaction manager, hashers, gateways to external systems.

Sending email is a gateway to an external system, the same category as `ITokenRepository`,
`IPasswordHasher<User>` and `ITransactionManager`, which `UserService` already injected; in
ports-and-adapters terms the service layer owns the port and Resend is the adapter. So the
direction of the dependency was right.

Agreed the implementation blurred that, though:
- Naming it `EmailService` and placing it next to the business services made it read as a
  peer of `UserService`
- It inherited `BaseService<T>` and implemented `IBaseService`, the marker for "a business
  service a controller can sit on top of", so nothing stopped a `BaseController<IEmailService>`

Offered two fixes: (1) rename and reclassify it inside the Services project (`IEmailSender` /
`ResendEmailSender`, no `BaseService` / `IBaseService`), or (2) move it to its own project
below Services, mirroring `PocketAdvisor.Repositories`. Advised against moving the call back
into the controller, since that orchestration is what caused the original finding and the
token leak.

---

## User Prompt 3

> Go with the 1 option and move it to a new folder inside the Services project.

---

## Assistant Response 3

- Renamed to `IEmailSender` / `ResendEmailSender` and moved them to
  `Senders/Interfaces` and `Senders/Implementations`, mirroring the existing
  `Interfaces` / `Implementations` layout
- Dropped `BaseService` / `IBaseService`; the class now takes `ILogger<ResendEmailSender>`
  directly with its own null check and property
- Updated `UserService` to depend on `IEmailSender`
- Registered it in `AddServices` in its own group above the business services
- Deleted the old `IEmailService.cs` / `EmailService.cs` (deletion had been reset, so asked
  for permission again)
- Updated the `DOCUMENTATION.md` paragraph to describe senders as gateways to external
  systems that cannot be exposed through a controller

---

## User Prompt 4

> Would it now be better to call it a helper?

---

## Assistant Response 4

Recommended against it. In C#, "helper" usually means stateless utility code with no
dependencies and no I/O, often static — like `CreateError` in `BaseService` or
`HandleFailure` in `BaseController`. The email class doesn't fit:
- It has injected dependencies (Resend client, two options classes, a logger)
- It performs network I/O against an external system, so it can fail, has side effects and
  should be faked in tests — "helper" hides exactly that
- It sits behind an interface so it can be swapped, which a helper wouldn't need

Also noted that "Helper" is on the usual list of vague names (Manager, Util, Processor).
Suggested keeping "Sender", with "Client" or "Gateway" as accurate alternatives.

---

## User Prompt 5

> Let's go with Client then

---

## Assistant Response 5

- Renamed to `IEmailClient` / `ResendEmailClient` and moved them to `Clients/Interfaces` and
  `Clients/Implementations`; namespaces became `PocketAdvisor.Services.Clients.*`
- Updated `UserService` (`IEmailClient emailClient` parameter, `EmailClient` property) and
  the registration in `AddServices`, re-sorting the `using` directives there
- Updated `DOCUMENTATION.md`, changing "wraps the Resend client" to "wraps the Resend SDK" so
  "client" didn't mean two things in one sentence
- Deleted the old `Senders` folder (asked for delete permission again)

Noticed the user had staged part of the changes themselves and left that staging untouched.

---

## User Prompt 6

> I have made some changes, please review the branch.

---

## Assistant Response 6

Found the branch had been rebased onto `dev` after PR #2 (the base service branch) merged,
with three commits: "Implemented the Resend email client.", "Updated the security audit
findings." and "Updated the documentation.". Reviewed the full diff against `dev`.

Should fix:
1. The `Resend` package reference had been removed from `PocketAdvisor.WebApplication.csproj`,
   but `AddResendClient` in the web project still used `ResendClient`, `ResendClientOptions`
   and `IResend` directly. It only compiled through the transitive reference from Services.
   Suggested either restoring the reference, or moving `AddResendClient` into Services (which
   needs `Microsoft.Extensions.Http` for `AddHttpClient`)

Nits:
2. `using` order: `Clients.Interfaces` was appended at the end in `UserService.cs`, and
   `Configurations` came before `Clients` in `ResendEmailClient.cs`
3. `IEmailClient` documented only `ArgumentNullException`, but
   `ArgumentException.ThrowIfNullOrWhiteSpace` also throws `ArgumentException` for empty or
   whitespace values
4. The success logs in `UserService` had moved after the email call, so a Resend failure
   would hide that the database write had already been committed

Confirmed the rest: controller only handles HTTP, `IUserService` returns plain `Result`,
`ResendEmailClient` stays outside `BaseService` / `IBaseService`, scoped client on a transient
`IResend` is a safe lifetime combination, docs read well, no leftover `EmailService` /
`Sender` references. Noted the prompt transcript commit was still missing.

---

## User Prompt 7

> Do the Microsoft.Extensions.Http fix. Fix the using order, the exception docs and log
> order.

---

## Assistant Response 7

- Added `Microsoft.Extensions.Http` (10.0.12, matching the other `Microsoft.Extensions.*`
  packages) to `Directory.Packages.props` and referenced it from
  `PocketAdvisor.Services.csproj`; flagged that the version couldn't be confirmed against
  NuGet from here
- Moved `AddResendClient` unchanged from the web project's `ServiceCollectionExtensions` into
  the Services one and removed `using Resend;` from the web project. `Program.cs` already
  imported `PocketAdvisor.Services.Extensions`, so the call site stayed the same. The web
  project now only mentions Resend as a configuration section name
- Restored alphabetical `using` order in `UserService.cs` and `ResendEmailClient.cs`
- Added `<exception cref="ArgumentException">` (empty or white-space values) to both
  `IEmailClient` methods
- Moved the success logs in `UserService.CreateUserAsync` and `ForgotPasswordAsync` back to
  directly after `CommitTransactionAsync`, before the email is sent

Hit the stale `.git/index.lock` problem again, this time caused by `git status`; asked for
delete permission, removed it, and switched to `git --no-optional-locks` for read-only git
commands so it wouldn't recur.

Still unverified by an actual build — flagged that `dotnet build` (which also restores the
new package) should be run before committing.
