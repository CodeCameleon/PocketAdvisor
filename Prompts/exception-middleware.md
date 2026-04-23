# BaseController JWT User ID & Exception Handling Middleware

**Date:** 2026-04-23

---

## Turn 1

### Prompt
> Create a new get property in the BaseController that return the currently logged in user id form the JWT.
> It should be error driven, not nullable, because it should enforce the use of the Authorize attribute.

### Thinking
Explored the codebase to understand the BaseController structure and how JWT claims are written
before making any changes.

Key findings:
- `BaseController<TService>` lives at `Backend/PocketAdvisor.WebApplication/Controllers/BaseController.cs`
- JWT generation in `UserService` uses `JwtRegisteredClaimNames.Sub` (`"sub"`) as the claim key for the user ID,
stored as a `Guid` string
- No existing auth helpers for reading claims
- `System.IdentityModel.Tokens.Jwt` v8.17.0 is already a project dependency, so `JwtRegisteredClaimNames` is
available without adding a new package

Design decisions:
- **Returns `Guid`, not `Guid?`** — the non-nullable return type is the contract. No consumer needs to null-check it.
- **Throws `UnauthorizedAccessException`** if the `sub` claim is absent, making a misconfigured
endpoint (missing `[Authorize]`) blow up loudly at runtime rather than silently passing a null/default GUID into
service logic where it could cause subtle data bugs.
- **`Guid.Parse` not `Guid.TryParse`** — error-driven. A malformed subject in a token is a bug worth crashing on.
- **No logging in development** — the thrown exception carries enough context on its own.

### Response

Added `using System.IdentityModel.Tokens.Jwt;` to the imports and added the `CurrentUserId`
property to `BaseController<TService>`:

**File:** `Backend/PocketAdvisor.WebApplication/Controllers/BaseController.cs`

```csharp
/// <summary>
/// The ID of the currently authenticated user, extracted from the JWT subject claim.
/// </summary>
/// <exception cref="UnauthorizedAccessException">
/// Thrown if the subject claim is missing. This indicates the endpoint is not protected
/// by the <see cref="Microsoft.AspNetCore.Authorization.AuthorizeAttribute" />.
/// </exception>
protected Guid CurrentUserId
{
    get
    {
        string? subject = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (subject is null)
        {
            throw new UnauthorizedAccessException(
                $"The '{JwtRegisteredClaimNames.Sub}' claim is missing. " +
                $"Ensure the endpoint is decorated with the [Authorize] attribute."
            );
        }

        return Guid.Parse(subject);
    }
}
```

---

## Turn 2

### Prompt
> Now create an error handling middleware. I should only return the message of the error when the application is
> in development mode. If it is in production, a generic error message should be use and the real error
> message should be logged.

### Thinking
Explored `Program.cs` and the WebApplication project structure before writing anything.

Key findings:
- No existing custom middleware — the `Middlewares/` folder did not exist
- Logging uses the standard ASP.NET Core `ILogger<T>` (no Serilog)
- Existing error responses use `ProblemDetails` (RFC 7807) via the `BadRequest()` override in `BaseController`,
so the middleware should match that format for consistency
- The middleware must be registered **first** in the pipeline to catch exceptions from all other middleware

Design decisions:
- **`IHostEnvironment` over `IWebHostEnvironment`** — the more abstract type is sufficient since
only `IsDevelopment()` is needed
- **Development path** — returns `exception.Message` directly in `ProblemDetails.Detail`; no logging,
since the response body is enough feedback while debugging
- **Production path** — generic user-facing message in `ProblemDetails.Detail`; calls `LogError` with the
full `exception` object (not just `.Message`) so the structured log captures the stack trace, inner exceptions,
and the request's `TraceIdentifier` for correlation
- **Response format** — `ProblemDetails` with `application/problem+json`, matching what `BaseController.BadRequest()`
already produces
- **Pipeline position** — registered first, before Swagger, HTTPS redirection, and authorization

### Response

Created a new file and registered it in `Program.cs`.

**New file:** `Backend/PocketAdvisor.WebApplication/Middlewares/ExceptionHandlingMiddleware.cs`

```csharp
namespace PocketAdvisor.WebApplication.Middlewares;

/// <summary>
/// Represents middleware that catches unhandled exceptions and returns a
/// structured <see cref="ProblemDetails" /> response.
/// In development the real exception message is surfaced; in production a
/// generic message is returned and the real message is logged as an error.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    #region Fields

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware" /> class.
    /// </summary>
    /// <param name="next">The next middleware delegate in the pipeline.</param>
    /// <param name="logger">The logger used to record exceptions in production.</param>
    /// <param name="environment">The host environment used to determine the current mode.</param>
    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    #endregion

    #region InvokeAsync

    /// <summary>
    /// Invokes the middleware, catching any unhandled exception thrown further down the pipeline.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    #endregion

    #region Private Helpers

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        string detail;

        if (_environment.IsDevelopment())
        {
            detail = exception.Message;
        }
        else
        {
            _logger.LogError(
                exception,
                "An unhandled exception occurred while processing the request. " +
                "TraceId: {TraceId}",
                context.TraceIdentifier
            );

            detail = "An unexpected error occurred. Please try again later.";
        }

        ProblemDetails problemDetails = new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = detail,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    #endregion
}
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Program.cs` — middleware registered first in the pipeline:

```csharp
// Adds the middleware for handling unhandled exceptions.
app.UseMiddleware<PocketAdvisor.WebApplication.Middlewares.ExceptionHandlingMiddleware>();

// Adds the middleware for Swagger generation.
app.UsePocketAdvisorSwagger();
```

---

## Turn 3

### Prompt
> Now I need you to export everything from this task. You should use the MD format, because it is safe for git.
> The exporting should include my prompts, your responses and thinking as well. Please try to keep
> them in chronological order.

### Thinking
The current session does not appear in `list_sessions` because it is the active session. Reconstructed the
full transcript from conversation context, which is complete and in order. Wrote the export as a single Markdown
file directly to the workspace folder so it is immediately accessible and git-trackable.
