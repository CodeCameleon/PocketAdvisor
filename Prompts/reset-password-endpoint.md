# Reset Password Endpoint

**Date:** 2026-04-25

---

## Turn 1

### User

> Next up is the endpoint for changing the password with the sent out token.
> Also create everything you need for it to work.

### Assistant Thinking

Explored the full codebase before writing any code. Key findings:

- **Preceding work:** The forgot-password endpoint (previous session) already created `ForgotPasswordRequest`,
  `ForgotPasswordRequestValidator`, `ForgotPasswordAsync` in service + interface, and the controller action.
  `ETokenType.PasswordReset`, `TokenSecretsOptions.Value.PasswordReset`, and
  `TokenExpirationsOptions.Value.PasswordResetMinutes`
  were all already wired up.
- **Token shape:** All tokens are `RandomNumberGenerator.GetBytes(32)` → `Convert.ToBase64String` = exactly 44 chars.
  `VerifyEmailRequestValidator` uses `MinimumLength(44)` + `MaximumLength(44)` — same pattern followed here.
- **Token lookup pattern:** Mirrors `VerifyEmailAsync` and `RefreshAsync`: HMAC-SHA256 hash the incoming plain token
  using the appropriate secret, query DB by hash + type with `asTracking: true` and `includes: [t => t.User!]`,
  check expiry, act, delete token, commit.
- **Password update pattern:** Uses `PasswordHasher.HashPassword` + `UserRepository.Update` — same as the
  rehash path inside `LoginAsync`.
- **Request model:** `Token` + `Password` + `ConfirmPassword`, mirroring `CreateUserRequest` for the password fields
  and `VerifyEmailRequest` for the token field.
- **New request validation messages needed:** `PasswordResetTokenRequired`, `PasswordResetTokenTooShort`,
  `PasswordResetTokenTooLong` — added to Requests `.resx` + Designer.
- **New service validation message needed:** `InvalidPasswordResetToken` — added to Services `.resx` + Designer.
- **Return type:** `Task<Result>` (no value), same as `VerifyEmailAsync`. Controller returns `204 No Content`.
- **Route:** `POST api/users/reset-password`.

Files touched: 8 modified, 2 created.

### Assistant Response

All changes follow the established patterns exactly. Here is what was created and modified:

---

#### Modified: `PocketAdvisor.Requests/Resources/ValidationMessages.resx`

Added entries:

```xml
<data name="PasswordResetTokenRequired" xml:space="preserve">
    <value>The password reset token is required.</value>
</data>
<data name="PasswordResetTokenTooShort" xml:space="preserve">
    <value>The password reset token must be at least 44 characters.</value>
</data>
<data name="PasswordResetTokenTooLong" xml:space="preserve">
    <value>The password reset token must not exceed 44 characters.</value>
</data>
```

---

#### Modified: `PocketAdvisor.Requests/Resources/ValidationMessages.Designer.cs`

Added generated properties:

```csharp
/// <summary>
///   Looks up a localized string similar to The password reset token is required..
/// </summary>
internal static string PasswordResetTokenRequired {
    get {
        return ResourceManager.GetString("PasswordResetTokenRequired", resourceCulture);
    }
}

/// <summary>
///   Looks up a localized string similar to The password reset token must be at least 44 characters..
/// </summary>
internal static string PasswordResetTokenTooShort {
    get {
        return ResourceManager.GetString("PasswordResetTokenTooShort", resourceCulture);
    }
}

/// <summary>
///   Looks up a localized string similar to The password reset token must not exceed 44 characters..
/// </summary>
internal static string PasswordResetTokenTooLong {
    get {
        return ResourceManager.GetString("PasswordResetTokenTooLong", resourceCulture);
    }
}
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.resx`

Added entry:

```xml
<data name="InvalidPasswordResetToken" xml:space="preserve">
    <value>The password reset token is invalid or has expired.</value>
</data>
```

---

#### Modified: `PocketAdvisor.Services/Resources/ValidationMessages.Designer.cs`

Added generated property:

```csharp
/// <summary>
///   Looks up a localized string similar to The password reset token is invalid or has expired..
/// </summary>
internal static string InvalidPasswordResetToken {
    get {
        return ResourceManager.GetString("InvalidPasswordResetToken", resourceCulture);
    }
}
```

---

#### New: `PocketAdvisor.Requests/Users/ResetPasswordRequest.cs`

```csharp
namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The request model for resetting a user's password using a password reset token.
/// </summary>
public sealed class ResetPasswordRequest
{
    /// <summary>
    /// The plain-text password reset token sent to the user's email address.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// The new password for the user.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// The confirmation of the new password.
    /// </summary>
    public string? ConfirmPassword { get; set; }
}
```

---

#### New: `PocketAdvisor.Requests/Users/ResetPasswordRequestValidator.cs`

```csharp
using FluentValidation;
using PocketAdvisor.Requests.Resources;

namespace PocketAdvisor.Requests.Users;

/// <summary>
/// The validator for the <see cref="ResetPasswordRequest" /> model.
/// </summary>
public sealed class ResetPasswordRequestValidator
    : AbstractValidator<ResetPasswordRequest>
{
    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordRequestValidator" /> class.
    /// </summary>
    public ResetPasswordRequestValidator()
    {
        RuleFor(rpr => rpr.Token).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordResetTokenRequired)
            .MinimumLength(44).WithMessage(ValidationMessages.PasswordResetTokenTooShort)
            .MaximumLength(44).WithMessage(ValidationMessages.PasswordResetTokenTooLong);

        RuleFor(rpr => rpr.Password).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong)
            .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.PasswordTooWeak);

        RuleFor(rpr => rpr.ConfirmPassword).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage(ValidationMessages.ConfirmPasswordRequired)
            .MaximumLength(100).WithMessage(ValidationMessages.ConfirmPasswordTooLong)
            .Equal(rpr => rpr.Password).WithMessage(ValidationMessages.ConfirmPasswordMismatch)
            .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.ConfirmPasswordTooWeak);
    }

    #endregion
}
```

---

#### Modified: `PocketAdvisor.Services/Interfaces/IUserService.cs`

Added method signature before `LoginAsync`:

```csharp
/// <summary>
/// Resets the password of a user using the supplied password reset token asynchronously.
/// </summary>
/// <param name="request">The password reset token and the new password presented by the client.</param>
/// <returns>
/// A task that represents the asynchronous operation. The task result contains a
/// <see cref="Result" /> indicating the success or failure of the operation.
/// </returns>
Task<Result> ResetPasswordAsync(ResetPasswordRequest request);
```

---

#### Modified: `PocketAdvisor.Services/Implementations/UserService.cs`

Added `#region ResetPasswordAsync` implementation before `LoginAsync`:

```csharp
/// <inheritdoc />
public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request)
{
    Logger.LogInformation("Resetting user password...");

    IValidator<ResetPasswordRequest> validator = GetValidator<ResetPasswordRequest>();
    ValidationResult validationResult = await validator.ValidateAsync(request);

    if (!validationResult.IsValid)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
        {
            Logger.LogWarning(
                "Validation failed for ResetPasswordRequest: {Errors}",
                validationResult.Errors
            );
        }

        return Result.Fail(validationResult.Errors.ToErrorList());
    }

    using HMACSHA256 hmacSha256 = new(Encoding.UTF8.GetBytes(TokenSecretsOptions.Value.PasswordReset));
    byte[] hashBytes = hmacSha256.ComputeHash(Encoding.UTF8.GetBytes(request.Token!));
    string incomingHash = Convert.ToBase64String(hashBytes);

    Token? existingToken = await TokenRepository.GetSingleOrDefaultAsync(
        t => t.Hash == incomingHash && t.Type == ETokenType.PasswordReset,
        asTracking: true,
        includes: [t => t.User!]
    );

    if (existingToken is null || existingToken.ExpiryAt <= DateTime.UtcNow)
    {
        return Result.Fail(
            CreateError(ValidationMessages.InvalidPasswordResetToken, nameof(request.Token))
        );
    }

    await TransactionManager.Value.BeginTransactionAsync();

    existingToken.User!.PasswordHash = PasswordHasher.HashPassword(existingToken.User, request.Password!);
    UserRepository.Update(existingToken.User);

    TokenRepository.Delete(existingToken);

    await TransactionManager.Value.CommitTransactionAsync();

    Logger.LogInformation("User password reset successfully.");
    return Result.Ok();
}
```

---

#### Modified: `PocketAdvisor.WebApplication/Controllers/UserController.cs`

Added `#region ResetPasswordAsync` endpoint between `ForgotPasswordAsync` and `VerifyEmailAsync`:

```csharp
#region ResetPasswordAsync

/// <summary>
/// Resets the password of a user using the supplied password reset token asynchronously.
/// </summary>
/// <param name="request">The password reset token and the new password presented by the client.</param>
[HttpPost("reset-password")]
[ProducesResponseType(typeof(void), StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> ResetPasswordAsync([FromBody] ResetPasswordRequest request)
{
    Result result = await Service.ResetPasswordAsync(request);

    if (result.IsFailed)
    {
        return BadRequest(result.Errors);
    }

    return NoContent();
}

#endregion
```

---

### Design Decisions

- **`204 No Content` return:** Mirrors `VerifyEmailAsync` — the endpoint performs a state mutation with no resource
  to return.
- **Token pinned to `nameof(request.Token)`:** Follows the same property-pinning convention used by
  `InvalidRefreshToken` and `InvalidEmailVerificationToken`.
- **Token deleted after use:** Single-use enforcement — once the password is reset the token is consumed,
  preventing replay attacks.

---

## Turn 2

### User

> Move the password validation method into a common place from the PasswordReset
> and CreateUser request. You can make it internal only.

### Assistant Thinking

- The `BeStrongPassword` private static method was duplicated verbatim in both `CreateUserRequestValidator` and
  `ResetPasswordRequestValidator`.
- Both classes live in `PocketAdvisor.Requests.Users` — no namespace crossing needed.
- The project has no existing `Helpers/` or `Validators/` subfolder. Since the method is user-domain-specific, a
  `PasswordValidator.cs` file directly in `Users/` was the natural home, keeping it co-located with its consumers.
- Declared `internal static` — accessible within the assembly, not exposed publicly.
- Both validator constructors updated to call `PasswordValidator.BeStrongPassword` instead of the local private method.
  Their private `BeStrongPassword` regions were removed entirely.

Files touched: 2 modified, 1 created.

### Assistant Response

---

#### New: `PocketAdvisor.Requests/Users/PasswordValidator.cs`

```csharp
namespace PocketAdvisor.Requests.Users;

/// <summary>
/// Provides shared password validation logic for user request validators.
/// </summary>
internal static class PasswordValidator
{
    /// <summary>
    /// Validates whether the given password satisfies the required strength policy.
    /// </summary>
    /// <param name="password">The password to validate.</param>
    /// <returns>
    /// <see langword="true" />, if the password is strong enough, <see langword="false" /> otherwise.
    /// </returns>
    internal static bool BeStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            return false;
        }

        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(ch => !char.IsLetterOrDigit(ch));

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
```

---

#### Modified: `PocketAdvisor.Requests/Users/CreateUserRequestValidator.cs`

Removed the private `BeStrongPassword` region. Both `Must(...)` calls now delegate to
`PasswordValidator.BeStrongPassword`:

```csharp
RuleFor(cur => cur.Password).Cascade(CascadeMode.Stop)
    .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
    .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong)
    .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.PasswordTooWeak);

RuleFor(cur => cur.ConfirmPassword).Cascade(CascadeMode.Stop)
    .NotEmpty().WithMessage(ValidationMessages.ConfirmPasswordRequired)
    .MaximumLength(100).WithMessage(ValidationMessages.ConfirmPasswordTooLong)
    .Equal(cur => cur.Password).WithMessage(ValidationMessages.ConfirmPasswordMismatch)
    .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.ConfirmPasswordTooWeak);
```

---

#### Modified: `PocketAdvisor.Requests/Users/ResetPasswordRequestValidator.cs`

Same treatment — private `BeStrongPassword` region removed, both `Must(...)` calls delegate to
`PasswordValidator.BeStrongPassword`:

```csharp
RuleFor(rpr => rpr.Password).Cascade(CascadeMode.Stop)
    .NotEmpty().WithMessage(ValidationMessages.PasswordRequired)
    .MaximumLength(100).WithMessage(ValidationMessages.PasswordTooLong)
    .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.PasswordTooWeak);

RuleFor(rpr => rpr.ConfirmPassword).Cascade(CascadeMode.Stop)
    .NotEmpty().WithMessage(ValidationMessages.ConfirmPasswordRequired)
    .MaximumLength(100).WithMessage(ValidationMessages.ConfirmPasswordTooLong)
    .Equal(rpr => rpr.Password).WithMessage(ValidationMessages.ConfirmPasswordMismatch)
    .Must(PasswordValidator.BeStrongPassword).WithMessage(ValidationMessages.ConfirmPasswordTooWeak);
```
