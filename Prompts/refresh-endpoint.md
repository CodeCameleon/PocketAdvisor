# JWT Refresh Endpoint

**Date:** 2026-04-23

---

## Prompt 1

> Create a refresh endpoint in the session controller where the frontend can renew the JWT token.

---

## Response 1

### Codebase Exploration

Before writing any code, an exploration agent audited the relevant parts of the codebase to understand the existing
auth architecture. Key findings:

**Project:** C# .NET 10 with a clean layered architecture (Controllers → Services → Repositories → Entities).

**Existing auth flow:**

- `POST /api/sessions/login` → `SessionController.LoginAsync` → `UserService.LoginAsync`
- On login, a 32-byte cryptographically random refresh token is generated, hashed with HMACSHA256 using
`TokenSecretsOptions.Value.Refresh` as the key, and stored in the `Token` table. The plain value is
returned to the client.
- A JWT is signed with HMAC SHA256, using `TokenSecretsOptions.Value.JsonWeb`, with `sub` (user ID) and `role`
claims. Expiry is configurable via `TokenExpirationsOptions.Value.JsonWebMinutes` (default 15 min).
- Refresh token expiry is `TokenExpirationsOptions.Value.RefreshDays` (default 14 days).

**Key files identified:**

| File                                   | Purpose                                                                     |
|----------------------------------------|-----------------------------------------------------------------------------|
| `SessionController.cs`                 | HTTP entry points (`api/sessions`)                                          |
| `UserService.cs`                       | Auth logic — JWT generation, token hashing, DB writes                       |
| `IUserService.cs`                      | Service interface                                                           |
| `Token.cs`                             | Entity: `Id`, `Hash`, `ExpiryAt`, `Type`, `UserId`, `User`                  |
| `ETokenType.cs`                        | Enum: `EmailVerification`, `PasswordReset`, `Refresh`                       |
| `TokenSecretsOptions.cs`               | Signing secrets config                                                      |
| `TokenExpirationsOptions.cs`           | Expiry durations config                                                     |
| `LoginRequest.cs` / `LoginResponse.cs` | Existing request/response DTOs                                              |
| `IBaseRepository.cs`                   | `CreateAsync`, `GetSingleOrDefaultAsync`, `Update`, `Delete`, `ExistsAsync` |

---

### Changes Made

#### 1. New file — `PocketAdvisor.Requests/Users/RefreshRequest.cs`

```csharp
namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for refreshing an expired JSON Web Token.
/// </summary>
public sealed class RefreshRequest
{
    /// <summary>
    /// The plain-text refresh token previously issued to the user.
    /// </summary>
    public string? RefreshToken { get; set; }
}
```

---

#### 2. New file — `PocketAdvisor.Requests/Users/RefreshRequestValidator.cs`

Initially created as:

```csharp
using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="RefreshRequest" /> model.
/// </summary>
public sealed class RefreshRequestValidator
    : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshRequestValidator" /> class.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(rr => rr.RefreshToken).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.RefreshTokenRequired);
    }
}
```

Subsequently updated (by linter/user) to also enforce the expected token length:

```csharp
using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="RefreshRequest" /> model.
/// </summary>
public sealed class RefreshRequestValidator
    : AbstractValidator<RefreshRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RefreshRequestValidator" /> class.
    /// </summary>
    public RefreshRequestValidator()
    {
        RuleFor(rr => rr.RefreshToken).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.RefreshTokenRequired)
            .MinimumLength(44).WithMessage(ValidationMessages.RefreshTokenTooShort)
            .MaximumLength(44).WithMessage(ValidationMessages.RefreshTokenTooLong);
    }
}
```

> **Note:** 44 characters is the exact length of a Base64-encoded 32-byte token
> (`Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))`), matching how the token is generated in
> `UserService.GenerateToken`.

---

#### 3. Modified — `PocketAdvisor.Requests/Resources/ValidationMessages.resx`

Added:

```xml
<data name="RefreshTokenRequired" xml:space="preserve">
    <value>The refresh token is required.</value>
</data>
```

> **Note:** `RefreshTokenTooShort` and `RefreshTokenTooLong` entries also need to be added to this `.resx`
> file and its `Designer.cs` to support the updated validator rules (length validation messages added in
> the linter pass).

---

#### 4. Modified — `PocketAdvisor.Requests/Resources/ValidationMessages.Designer.cs`

Added property:

```csharp
/// <summary>
///   Looks up a localized string similar to The refresh token is required..
/// </summary>
internal static string RefreshTokenRequired {
    get {
        return ResourceManager.GetString("RefreshTokenRequired", resourceCulture);
    }
}
```

---

#### 5. Modified — `PocketAdvisor.Services/Resources/ValidationMessages.resx`

Added:

```xml
<data name="InvalidRefreshToken" xml:space="preserve">
    <value>The refresh token is invalid or has expired.</value>
</data>
```

---

#### 6. Modified — `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`

Added property:

```csharp
/// <summary>
///   Looks up a localized string similar to The refresh token is invalid or has expired..
/// </summary>
internal static string InvalidRefreshToken {
    get {
        return ResourceManager.GetString("InvalidRefreshToken", resourceCulture);
    }
}
```

---

#### 7. Modified — `PocketAdvisor.Services/Interfaces/IUserService.cs`

Added method to the interface:

```csharp
/// <summary>
/// Validates a refresh token, rotates it, and issues a new JSON Web Token and refresh token asynchronously.
/// </summary>
/// <param name="request">The refresh token presented by the client.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result{TValue}" /> indicating the success or failure of the operation.
/// If successful, the result contains a <see cref="LoginResponse" /> with the newly issued tokens.
/// </returns>
Task<Result<LoginResponse>> RefreshAsync(RefreshRequest request);
```

---

#### 8. Modified — `PocketAdvisor.Services/Implementations/UserService.cs`

Added `RefreshAsync` implementation (inserted before the existing `GenerateJsonWebToken` region):

```csharp
#region RefreshAsync

/// <inheritdoc />
public async Task<Result<LoginResponse>> RefreshAsync(RefreshRequest request)
{
    Logger.LogInformation("Refreshing session...");

    IValidator<RefreshRequest> validator = GetValidator<RefreshRequest>();
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Validation failed for RefreshRequest: {Errors}",
                validationResult.Errors
            );
        }

        return Result.Fail(validationResult.Errors.ToErrorList());
    }

    using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.Refresh));
    byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(request.RefreshToken!));
    string incomingHash = Convert.ToBase64String(hashBytes);

    Token? existingToken = await TokenRepository.GetSingleOrDefaultAsync(
        t => t.Hash == incomingHash && t.Type == ETokenType.Refresh,
        asTracking: true,
        includes: [t => t.User!]
    );

    if (existingToken is null || existingToken.ExpiryAt <= DateTime.UtcNow || existingToken.User is null)
    {
        return Result.Fail(ValidationMessages.InvalidRefreshToken);
    }

    User user = existingToken.User;

    await TransactionManager.Value.BeginTransactionAsync();

    TokenRepository.Delete(existingToken);

    GeneratedToken generatedRefreshToken = GenerateToken(TokenSecretsOptions.Value.Refresh);

    Token newRefreshToken = new()
    {
        Hash = generatedRefreshToken.Hash,
        ExpiryAt = DateTime.UtcNow.AddDays(TokenExpirationsOptions.Value.RefreshDays),
        Type = ETokenType.Refresh,
        UserId = user.Id
    };
    await TokenRepository.CreateAsync(newRefreshToken);

    await TransactionManager.Value.CommitTransactionAsync();

    Logger.LogInformation("Session refreshed successfully.");

    return Result.Ok(new LoginResponse
    {
        JsonWebToken = GenerateJsonWebToken(user),
        RefreshToken = generatedRefreshToken.Plain
    });
}

#endregion
```

**Design decisions:**

- The incoming plain token is hashed with the same HMACSHA256 + Refresh secret before the DB lookup — the DB
never holds plain tokens.
- The old refresh token is **deleted** before the new one is inserted within the same transaction (token rotation).
A stolen token can only be used once.
- The `User` navigation property is eager-loaded via `includes` so a second DB round-trip is avoided when calling
`GenerateJsonWebToken(user)`.
- If the token is not found, is expired, or has no associated user, the same generic `InvalidRefreshToken` error is
returned to avoid leaking information.

---

#### 9. Modified — `PocketAdvisor.WebApplication/Controllers/SessionController.cs`

Added the `RefreshAsync` endpoint:

```csharp
#region RefreshAsync

/// <summary>
/// Validates a refresh token, rotates it, and issues a new JSON Web Token and refresh token asynchronously.
/// </summary>
/// <param name="request">The refresh token presented by the client.</param>
[HttpPost("refresh")]
[ProducesResponseType(typeof(LoginResponse), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> RefreshAsync([FromBody] RefreshRequest request)
{
    Result<LoginResponse> result = await Service.RefreshAsync(request);

    if (result.IsFailed)
    {
        return BadRequest(result.Errors);
    }

    return StatusCode(StatusCodes.Status201Created, result.Value);
}

#endregion
```

---

### API Contract

**Endpoint:** `POST /api/sessions/refresh`

**Request body:**
```json
{
  "refreshToken": "<plain-text refresh token from login or previous refresh>"
}
```

**Success — 201 Created:**
```json
{
  "jsonWebToken": "<new JWT>",
  "refreshToken": "<new refresh token — store and replace the old one>"
}
```

**Failure — 400 Bad Request:**
```json
[
  { "message": "The refresh token is invalid or has expired." }
]
```

---

## Prompt 2

> Now I need you to export everything from this task. You should use the MD format, because it is safe for git. The
> exporting should include my prompts, your responses and thinking as well. Please try to keep them in
> chronological order.

---

## Response 2

This document — `refresh-endpoint.md` — is that export.
