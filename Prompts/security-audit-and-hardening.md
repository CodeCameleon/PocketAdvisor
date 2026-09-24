# Security Audit & Hardening Sprint

**Date:** 2026-09-21

---

## Turn 1

### Prompt
> Először is kérlek térképezd fel a projektet.
> *(Please map the project first.)*

### Thinking
Read the full project tree, all backend controllers, services, extensions, middleware, and
the Angular frontend structure before forming any opinion.

Key findings recorded across the codebase:
- **Swagger** registered unconditionally — available in production
- **No rate limiting** on any endpoint — login, register, refresh, forgot-password all open
- **JWT + refresh token in `localStorage`** — XSS exfiltration risk
- **`secrets.bin` committed to git** — encrypted, but still tracked in history
- **User enumeration** in `ForgotPasswordAsync` — distinct 404 on unknown email
- **Email sent from controller** — infrastructure in presentation layer
- **`GetAccountsAsync`** loads all transactions into memory for balance calculation
- **`Categories` unique index** missing `NULLS NOT DISTINCT` for global categories
- **`BaseService`** uses `IServiceProvider` (service locator anti-pattern)
- **Cross-currency transfers** accepted silently without validation or conversion
- **No "resend verification" endpoint**
- **No `environment.prod.ts`** — API URL hardcoded to `http://localhost:5078/api`
- **SSR remnants** (`serve:ssr` script, no `@angular/ssr` dependency)
- **Zero automated tests** — no `.spec.ts`, no xUnit project, no CI

### Response
Delivered a full audit grouped by severity (Critical → High → Medium → Low).
Full findings recorded in `Documentation/AUDIT.md`.

---

## Turn 2

### Prompt
> Kezdjük a Swagger javítással, hogy csak Development-ben legyen elérhető minden része.
> *(Let's start with the Swagger fix so all parts are only accessible in Development.)*

### Thinking
Read `SwaggerExtensions.cs` and `Program.cs`.

Both `AddPocketAdvisorSwagger` and `UsePocketAdvisorSwagger` were unconditional. The fix
must gate both the service registration (so `AddSwaggerGen` is never called outside
Development) and the middleware registration (so neither `UseSwagger()` nor `UseSwaggerUI()`
is added). If only the middleware is gated, the generator services are still registered
wastefully; if only the services are gated, the middleware registration silently no-ops
but is confusing.

Decided to pass `IHostEnvironment` into `AddPocketAdvisorSwagger` so it can return early
before touching the `IServiceCollection`, matching the pattern used by other conditional
service registrations in the project.

### Response

**File:** `Backend/PocketAdvisor.WebApplication/Extensions/SwaggerExtensions.cs`

```csharp
/// <summary>
/// Adds the Swagger generation services to the service collection.
/// <br />
/// Only registers services when the application is running in the Development environment.
/// </summary>
public static void AddPocketAdvisorSwagger(this IServiceCollection services, IHostEnvironment environment)
{
    if (!environment.IsDevelopment())
    {
        return;
    }

    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen(options => { /* existing options unchanged */ });
}

/// <summary>
/// Adds the Swagger middleware to the application pipeline.
/// <br />
/// Only registers middleware when the application is running in the Development environment.
/// </summary>
public static void UsePocketAdvisorSwagger(this WebApplication application)
{
    if (!application.Environment.IsDevelopment())
    {
        return;
    }

    application.UseSwagger();
    application.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(SwaggerUrl, SwaggerName);
    });
}
```

**File:** `Backend/PocketAdvisor.WebApplication/Program.cs`

```csharp
builder.Services.AddPocketAdvisorSwagger(builder.Environment);
```

---

## Turn 3

### Prompt
> Kicsit módosítottam a változtatásaidon, kérlek nézd meg őket, hogy minden rendben van-e
> és minden konzisztens a projekt többi részével.
> *(I modified your changes a bit; please review them to ensure everything is fine and
> consistent with the rest of the project.)*

### Thinking
Staged and read the modified `SwaggerExtensions.cs`. The user added `<br />` after the
first `<summary>` line in both XML doc comments, consistent with the convention used on
`AddPocketAdvisorCors` and `UsePocketAdvisorCors` in `ServiceCollectionExtensions.cs`.

All other changes were structurally identical to what was proposed.

### Response
Review passed. Changes are consistent with the project's XML doc convention. Committed.

---

## Turn 4

### Prompt
> Megcsináltam a commitot, most mehet a RateLimiter javítás.
> *(I've made the commit, now let's proceed with the RateLimiter fix.)*

### Thinking
Requirements gathered before writing any code:
- Sliding-window rate limiting (not fixed window) — better burst protection at window edges
- Two policies: `Authentication` (5 req/60 s/IP) and `Refresh` (30 req/60 s/IP)
- Partition key: `RemoteIpAddress` (IPv4 and IPv6 clients treated separately)
- `QueueLimit = 0` — reject immediately, no queuing
- `SegmentsPerWindow = 6` — 10-second granularity inside the 60-second window
- `AutoReplenishment = true` — background replenishment without explicit `.TryReplenish()` calls
- Config values read from `appsettings.json` via `IConfiguration`, validated at startup
  (missing or non-positive value throws `InvalidOperationException`)
- `429` response must include `Retry-After` header when the lease expiry is known
- Response body must be `application/problem+json` consistent with `ExceptionHandlingMiddleware`

Decided to:
1. Mirror `CorsPolicyNames.cs` with a new `RateLimiterPolicyNames.cs`
2. Add three config getters to `ConfigurationExtensions.cs` backed by a
   `GetRequiredPositiveInt32` helper
3. Add `AddPocketAdvisorRateLimiter` to `ServiceCollectionExtensions.cs`
4. Apply `[EnableRateLimiting]` at action level on `SessionController` and at class level
   on `UserController`
5. Add `[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]`
   to every rate-limited action for Swagger accuracy
6. Document the 429 handler in `appsettings.json` under `RateLimiting`

### Response

**New file:** `Backend/PocketAdvisor.WebApplication/Constants/RateLimiterPolicyNames.cs`

```csharp
namespace PocketAdvisor.WebApplication.Constants;

/// <summary>
/// Contains the names of the rate limiter policies registered in the application.
/// </summary>
public static class RateLimiterPolicyNames
{
    public const string Authentication = "PocketAdvisorAuthenticationRateLimiterPolicy";
    public const string Refresh = "PocketAdvisorRefreshRateLimiterPolicy";
}
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Extensions/ConfigurationExtensions.cs`

New constants (alphabetical order):
```csharp
private const string AuthenticationPermitLimit = "AuthenticationPermitLimit";
private const string NotPositiveIntegerMessageTemplate = "The value of '{0}' must be a positive integer.";
private const string RateLimiting = "RateLimiting";
private const string RefreshPermitLimit = "RefreshPermitLimit";
private const string WindowSeconds = "WindowSeconds";
```

New public getters:
```csharp
public static int GetRateLimitingAuthenticationPermitLimit(this IConfiguration configuration)
    => configuration.GetRequiredPositiveInt32(RateLimiting, AuthenticationPermitLimit);

public static int GetRateLimitingRefreshPermitLimit(this IConfiguration configuration)
    => configuration.GetRequiredPositiveInt32(RateLimiting, RefreshPermitLimit);

public static int GetRateLimitingWindowSeconds(this IConfiguration configuration)
    => configuration.GetRequiredPositiveInt32(RateLimiting, WindowSeconds);
```

New private helper:
```csharp
private static int GetRequiredPositiveInt32(this IConfiguration configuration,
    string sectionName, string key)
{
    int? value = configuration.GetSection(sectionName).GetValue<int?>(key);
    if (value is null) throw CreateInvalidOperationException(sectionName, key);
    if (value <= 0)
    {
        string message = string.Format(NotPositiveIntegerMessageTemplate,
            string.Join(Colon, sectionName, key));
        throw new InvalidOperationException(message);
    }
    return value.Value;
}
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Extensions/ServiceCollectionExtensions.cs`

New constants:
```csharp
private const int SegmentsPerWindow = 6;
private const string TooManyRequestsMessage = "Too many requests. Please try again later.";
private const string TooManyRequestsTitle = "Too Many Requests";
private const string TooManyRequestsType = "https://tools.ietf.org/html/rfc6585#section-4";
private const string UnknownPartitionKey = "Unknown";
private static readonly JsonSerializerOptions JsonSerializerOptions =
    new(JsonSerializerDefaults.Web);
```

New extension method:
```csharp
public static void AddPocketAdvisorRateLimiter(this IServiceCollection services,
    int authenticationPermitLimit, int refreshPermitLimit, int windowSeconds)
{
    services.AddRateLimiter(options =>
    {
        options.AddPolicy(RateLimiterPolicyNames.Authentication,
            context => CreatePartition(context, authenticationPermitLimit, windowSeconds));
        options.AddPolicy(RateLimiterPolicyNames.Refresh,
            context => CreatePartition(context, refreshPermitLimit, windowSeconds));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = OnRejectedAsync;
    });

    // The built-in client error mapping has no entry for 429, so the problem details
    // responses of the rate limiter would be missing their title and type.
    services.Configure<ApiBehaviorOptions>(options =>
    {
        options.ClientErrorMapping[StatusCodes.Status429TooManyRequests] = new()
        {
            Link = TooManyRequestsType,
            Title = TooManyRequestsTitle
        };
    });
}
```

Partition factory:
```csharp
private static RateLimitPartition<string> CreatePartition(
    HttpContext context, int permitLimit, int windowSeconds)
{
    string partitionKey =
        context.Connection.RemoteIpAddress?.ToString() ?? UnknownPartitionKey;

    return RateLimitPartition.GetSlidingWindowLimiter(partitionKey, _ => new()
    {
        PermitLimit = permitLimit,
        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        QueueLimit = 0,
        Window = TimeSpan.FromSeconds(windowSeconds),
        SegmentsPerWindow = SegmentsPerWindow,
        AutoReplenishment = true
    });
}
```

Rejection handler:
```csharp
private static async ValueTask OnRejectedAsync(
    OnRejectedContext context, CancellationToken cancellationToken)
{
    HttpContext httpContext = context.HttpContext;

    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
    {
        httpContext.Response.Headers.RetryAfter =
            ((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
    }

    ProblemDetailsFactory factory =
        httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

    ProblemDetails problemDetails = factory.CreateProblemDetails(
        httpContext,
        StatusCodes.Status429TooManyRequests,
        detail: TooManyRequestsMessage);

    httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
    httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;

    await httpContext.Response.WriteAsync(
        JsonSerializer.Serialize(problemDetails, JsonSerializerOptions),
        cancellationToken);
}
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Program.cs`

```csharp
builder.Services.AddPocketAdvisorRateLimiter(
    builder.Configuration.GetRateLimitingAuthenticationPermitLimit(),
    builder.Configuration.GetRateLimitingRefreshPermitLimit(),
    builder.Configuration.GetRateLimitingWindowSeconds()
);
// …
app.UseCors(CorsPolicyNames.Global);
app.UseRateLimiter();
app.UseAuthentication();
```

**Updated:** `appsettings.json` and `appsettings.Development.json`

```json
"RateLimiting": {
  "AuthenticationPermitLimit": 5,
  "RefreshPermitLimit": 30,
  "WindowSeconds": 60
}
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Controllers/SessionController.cs`

```csharp
[HttpPost("login")]
[EnableRateLimiting(RateLimiterPolicyNames.Authentication)]
[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public async Task<IActionResult> LoginAsync(...)

[HttpPost("refresh")]
[EnableRateLimiting(RateLimiterPolicyNames.Refresh)]
[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public async Task<IActionResult> RefreshAsync(...)
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Controllers/UserController.cs`

```csharp
[Route("api/users")]
[EnableRateLimiting(RateLimiterPolicyNames.Authentication)]
[ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
public sealed class UserController : BaseController<IUserService>
```

**Updated:** `Frontend/src/app/core/services/api-error.ts`

```typescript
if (error.status === 429) {
  return 'Too many requests. Please wait a moment and try again.';
}
```

---

## Turn 5

### Prompt
> Az InvalidValue elnevezés elég félrevezető egy pozitív szám üzenetnél vagy az üzenet
> legyen más vagy a változó legyen átnevezve.
> *(The InvalidValue name is quite misleading for a positive integer message — either
> the message should be different or the variable should be renamed.)*

### Thinking
The constant `InvalidValueMessageTemplate` held the message
`"The value of '{0}' must be a positive integer."`. The message is already accurate; the
constant name was the problem. `NotPositiveIntegerMessageTemplate` is more precise than
`NonPositiveIntegerMessageTemplate` (which implies ≤ 0 but not necessarily missing) and
matches the style of `KeyNotFoundMessageTemplate` already present in the file.

Also moved the constant to its correct alphabetical position: after `KeyNotFoundMessageTemplate`
and before the new `RateLimiting` constant.

### Response

Renamed `InvalidValueMessageTemplate` → `NotPositiveIntegerMessageTemplate` in
`ConfigurationExtensions.cs` and moved it to the correct alphabetical slot. The single
call site (`GetRequiredPositiveInt32`) was updated accordingly.

---

## Turn 6

### Prompt
> Csináltam pár saját módosítást is, kérlek nézd át őket, hogy minden rendben van-e.
> *(I made a few of my own modifications too, please review them to ensure everything
> is fine.)*

### Thinking
Staged and read the modified files. User changes:

1. `ServiceCollectionExtensions.cs` — summary for `AddPocketAdvisorRateLimiter` simplified
   to a single line (removed the `<br />` and `Both policies…` second sentence). Cleaner,
   no information lost. ✓
2. `SessionController.cs` — `[EnableRateLimiting]` moved from class level to action level
   on `LoginAsync` and `RefreshAsync`. More explicit, allows different future policies per
   action. ✓
3. `UserController.cs` — `[Route]` appears before `[EnableRateLimiting]` (differs from
   `CategoryController` where API-behaviour attributes come first). Minor ordering deviation,
   not a functional issue. Flagged but accepted.
4. `CreatePartition` — property initializer order updated to match the SDK documentation
   order: `PermitLimit`, `QueueProcessingOrder`, `QueueLimit`, `Window`,
   `SegmentsPerWindow`, `AutoReplenishment`. ✓

### Response
All changes approved. The attribute ordering in `UserController` is a minor style deviation
from `CategoryController`; flagged for awareness, not a correctness issue.

---

## Turn 7

### Prompt
> Értem, kérlek a dokumentációt javítsd te, a többit megcsináltam én.
> *(I understand; please fix the documentation yourself, I've done the rest.)*

### Thinking
Read `Documentation/DOCUMENTATION.md` in full. Identified all sections that needed updating:

- Technology stack table: missing rate limiting row
- Section 6: no rate limiting subsection
- Section 8 REST status codes: 429 not listed
- Section 8 Security: rate limiting not mentioned
- Two references to Swagger being available in "all environments" needed correction

### Response

Updated `Documentation/DOCUMENTATION.md`:

- Technology stack table: added `ASP.NET Core Rate Limiting | Sliding-window middleware` row
- Section 6: added `### Rate limiting` subsection with policy table, refresh limit rationale,
  startup validation note, 429 response contract (body + `Retry-After` header), and
  reverse-proxy deployment note (`ForwardedHeadersMiddleware`)
- Section 8 REST: added `429 Too Many Requests` to the status code list
- Section 8 Security: added rate limiting to the security measures bullet list
- Two Swagger mentions corrected from "all environments" → "development environment only"

---

## Turn 8

### Prompt
*(User pasted two JSON payloads showing the difference between a 500 and a 429 response.)*

```json
// 500 — has type and title
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.6.1",
  "title": "An error occurred while processing your request.",
  "status": 500,
  "detail": "...",
  "traceId": "..."
}

// 429 — missing type and title
{
  "status": 429,
  "detail": "Too many requests. Please try again later.",
  "traceId": "..."
}
```

> Can you make them look the same?

### Thinking
Root cause: `ProblemDetailsFactory.CreateProblemDetails` populates `type` and `title` from
`ApiBehaviorOptions.ClientErrorMapping`, keyed by status code. The framework ships entries
for RFC 9110 codes (400, 401, 403, 404, 409, 415, 422, 500, …) but **not for 429**, which
was introduced by RFC 6585. Because no entry existed, both fields were `null`, and
`[JsonIgnore(WhenWritingNull)]` on the `ProblemDetails` model suppressed them from the
serialized output entirely.

The fix belongs at the registration site, not in `OnRejectedAsync`. Registering the mapping
once in `AddPocketAdvisorRateLimiter` means any future code path that produces a 429
(including `Problem(429)` in a controller) gets the correct fields automatically.

The `type` URL points to RFC 6585 §4, consistent with how the framework always links to
the RFC section that defines the specific status code.

### Response

Added two constants and a `Configure<ApiBehaviorOptions>` call inside
`AddPocketAdvisorRateLimiter` in `ServiceCollectionExtensions.cs`:

```csharp
private const string TooManyRequestsTitle = "Too Many Requests";
private const string TooManyRequestsType = "https://tools.ietf.org/html/rfc6585#section-4";

// Inside AddPocketAdvisorRateLimiter, after services.AddRateLimiter(…):
services.Configure<ApiBehaviorOptions>(options =>
{
    options.ClientErrorMapping[StatusCodes.Status429TooManyRequests] = new()
    {
        Link = TooManyRequestsType,
        Title = TooManyRequestsTitle
    };
});
```

Expected 429 response after the fix:

```json
{
  "type": "https://tools.ietf.org/html/rfc6585#section-4",
  "title": "Too Many Requests",
  "status": 429,
  "detail": "Too many requests. Please try again later.",
  "traceId": "00-..."
}
```

`OnRejectedAsync` and the documentation were not changed — the existing docs already
promised a `ProblemDetails` body.
