# Forgot Password Endpoint

**Date:** 2026-04-25

---

## Turn 1

### User

> Create a new endpoint for sending out the password reset email. You can find the template id of the
> password reset email inside the user controller. It has two variables, Minutes and Url. You will
> also need to create the request model and service method for it as well.

### Assistant Thinking

Explored the full codebase before writing any code. Key findings:

- **Project structure:** Clean Architecture solution with 8 projects: Enums, Entities, DbContexts, Repositories,
Requests, Responses, Services, WebApplication.
- **Template ID:** `PasswordResetTemplateId = Guid.Parse("4f196197-f7e1-4724-bdf4-7540c27bdaab")` — already declared
as a constant in `UserController.cs`.
- **Template variables:** `Minutes` (constant already defined at line 28) and `Url` (constant at line 33) — both
already present in the controller.
- **URL building:** Follows the same `"{0}{1}?token={2}"` pattern used by email verification, with
`FrontendOptions.Value.BaseUrl` + `FrontendOptions.Value.PasswordResetPath`.
- **Token expiry config:** `TokenExpirationsOptions.Value.PasswordResetMinutes` exists in `TokenExpirationsOptions.cs`.
- **Token secret config:** `TokenSecretsOptions.Value.PasswordReset` exists in `TokenSecretsOptions.cs`.
- **Token type:** `ETokenType.PasswordReset = 2` already defined in the enum.
- **Request model pattern:** Single sealed class with nullable property, validated by a companion
`AbstractValidator<T>` class in the same folder.
- **Service pattern:** Validate → normalize email → DB lookup → begin transaction → create token → commit →
return plain token. Mirrors `CreateUserAsync`.
- **Controller pattern:** Call service → check `IsFailed` → build `EmailMessage` with template variables →
`Resend.EmailSendAsync` → return status code.
- **Existing email messages reused:** `EmailRequired`, `EmailTooLong`, `EmailInvalid` from Requests `.resx` —
no new request-side messages needed.
- **New service message needed:** `UserNotFound` — added to Services `.resx` and its Designer.

Files touched: 6 modified, 2 created.

### Assistant Response

All changes follow the established patterns exactly. Here is what was created and modified:

---

#### New: `PocketAdvisor.Requests/Users/ForgotPasswordRequest.cs`

```csharp
namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for initiating a password reset by sending a reset email.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>
    /// The email address of the user requesting a password reset.
    /// </summary>
    public string? Email { get; set; }
}
```

---

#### New: `PocketAdvisor.Requests/Users/ForgotPasswordRequestValidator.cs`

```csharp
using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="ForgotPasswordRequest" /> model.
/// </summary>
public sealed class ForgotPasswordRequestValidator
    : AbstractValidator<ForgotPasswordRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ForgotPasswordRequestValidator" /> class.
    /// </summary>
    public ForgotPasswordRequestValidator()
    {
        RuleFor(fpr => fpr.Email).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.EmailRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.EmailTooLong)
            .EmailAddress().WithMessage(ValidationMessages.EmailInvalid);
    }
}
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.resx`

Added entry:

```xml
<data name="UserNotFound" xml:space="preserve">
    <value>No account associated with the provided email address was found.</value>
</data>
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`

Added generated property:

```csharp
/// <summary>
///   Looks up a localized string similar to No account associated with the provided email address was found..
/// </summary>
internal static string UserNotFound {
    get {
        return ResourceManager.GetString("UserNotFound", resourceCulture);
    }
}
```

---

#### Modified: `PocketAdvisor.Services/Interfaces/IUserService.cs`

Added method signature:

```csharp
/// <summary>
/// Generates a password reset token for the given email address and returns it asynchronously.
/// </summary>
/// <param name="request">The email address of the user requesting a password reset.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result{TValue}" /> indicating the success or failure of the operation.
/// If successful, the result contains the plain-text password reset token.
/// </returns>
Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequest request);
```

---

#### Modified: `PocketAdvisor.Services/Implementations/UserService.cs`

Added `#region ForgotPasswordAsync` implementation before `GenerateJsonWebToken`:

```csharp
/// <inheritdoc />
public async Task<Result<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
{
    Logger.LogInformation("Processing forgot password request...");

    IValidator<ForgotPasswordRequest> validator = GetValidator<ForgotPasswordRequest>();
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Validation failed for ForgotPasswordRequest: {Errors}",
                validationResult.Errors
            );
        }

        return Result.Fail(validationResult.Errors.ToErrorList());
    }

    string normalizedEmail = request.Email!.Trim().ToLowerInvariant();

    User? user = await UserRepository.GetSingleOrDefaultAsync(
        u => u.Email == normalizedEmail
    );

    if (user is null)
    {
        return Result.Fail(
            CreateError(ValidationMessages.UserNotFound, nameof(request.Email))
        );
    }

    await TransactionManager.Value.BeginTransactionAsync();

    GeneratedToken generatedToken = GenerateToken(TokenSecretsOptions.Value.PasswordReset);

    Token token = new()
    {
        Hash = generatedToken.Hash,
        ExpiryAt = DateTime.UtcNow.AddMinutes(TokenExpirationsOptions.Value.PasswordResetMinutes),
        Type = ETokenType.PasswordReset,
        UserId = user.Id
    };
    await TokenRepository.CreateAsync(token);

    await TransactionManager.Value.CommitTransactionAsync();

    Logger.LogInformation("Password reset token generated successfully.");
    return Result.Ok(generatedToken.Plain);
}
```

---

#### Modified: `PocketAdvisor.WebApplication/Controllers/UserController.cs`

Added `PasswordResetUrlTemplate` constant and `#region ForgotPasswordAsync` endpoint between `CreateUserAsync` and
`VerifyEmailAsync`:

```csharp
/// <summary>
/// The template used to build the password reset URL.
/// </summary>
private const string PasswordResetUrlTemplate = "{0}{1}?token={2}";
```

```csharp
#region ForgotPasswordAsync

/// <summary>
/// Sends a password reset email to the user with the given email address asynchronously.
/// </summary>
/// <param name="request">The email address of the user requesting a password reset.</param>
[HttpPost("forgot-password")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ForgotPasswordAsync([FromBody] ForgotPasswordRequest request)
{
    Result<string> result = await Service.ForgotPasswordAsync(request);

    if (result.IsFailed)
    {
        return BadRequest(result.Errors);
    }

    EmailMessage emailMessage = new()
    {
        From = string.Empty, // This is defined in the template.
        To = request.Email!,
        Subject = string.Empty, // This is defined in the template too.
        Template = new()
        {
            TemplateId = PasswordResetTemplateId,
            Variables = new()
            {
                {
                    Minutes, TokenExpirationsOptions.Value.PasswordResetMinutes
                },
                {
                    Url, string.Format(
                        PasswordResetUrlTemplate,
                        FrontendOptions.Value.BaseUrl,
                        FrontendOptions.Value.PasswordResetPath,
                        result.Value
                    )
                }
            }
        }
    };
    await Resend.EmailSendAsync(emailMessage);

    return NoContent();
}

#endregion
```

---

### Design Decisions

- **`204 No Content` instead of `201 Created`:** The endpoint triggers a side-effect (sending an email) rather than
creating a primary resource. `204` matches the pattern used by `VerifyEmailAsync` for similar
fire-and-acknowledge operations.
- **`UserNotFound` error pinned to `nameof(request.Email)`:** Follows the same property-pinning pattern as
`EmailAlreadyExists` and `EmailNotVerified`, so the validation problem details response correctly identifies
which field caused the failure.
- **No new request-level validation messages:** The validator reuses the existing `EmailRequired`, `EmailTooLong`,
and `EmailInvalid` messages from the Requests `.resx` — the semantics are identical, so duplication was avoided.
- **`PasswordResetUrlTemplate` kept as a separate constant:** Even though its value is identical to
`EmailVerificationUrlTemplate`, having a named constant makes the intent clear and insulates the password reset
path from any future change to the email verification URL shape.
