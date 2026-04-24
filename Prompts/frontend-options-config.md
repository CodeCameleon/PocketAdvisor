# Frontend Options Configuration

**Date:** 2026-04-24

---

## Prompt 1

**User:**
I'm going to need to store the frontend domain and some routes like the email verification and the password reset
path inside the appsettings.json file. Create the needed configuration classes for it.

**Assistant:**
Created the following:

**New file — `PocketAdvisor.Services/Configurations/FrontendOptions.cs`**
```csharp
using System.ComponentModel.DataAnnotations;

namespace PocketAdvisor.Services.Configurations;

/// <summary>
/// Represents the frontend-related settings bound from configuration.
/// </summary>
public sealed class FrontendOptions
    : IBaseOptions
{
    /// <inheritdoc />
    public static string SectionName => "Frontend";

    /// <summary>
    /// The base URL of the frontend application.
    /// </summary>
    [Required]
    public required string BaseUrl { get; init; }

    /// <summary>
    /// The path appended to <see cref="BaseUrl" /> for the email verification page.
    /// </summary>
    [Required]
    public required string EmailVerificationPath { get; init; }

    /// <summary>
    /// The path appended to <see cref="BaseUrl" /> for the password reset page.
    /// </summary>
    [Required]
    public required string PasswordResetPath { get; init; }
}
```

**Updated — `ServiceCollectionExtensions.cs`**
Added `services.AddOptions<FrontendOptions>(configuration);` to the `AddServices` method.

**Updated — `appsettings.json`**
```json
"Frontend": {
  "BaseUrl": "https://pocketadvisor.codecameleon.com",
  "EmailVerificationPath": "/auth/verify-email",
  "PasswordResetPath": "/auth/reset-password"
}
```

**Updated — `appsettings.Development.json`**
```json
"Frontend": {
  "BaseUrl": "http://localhost:3000",
  "EmailVerificationPath": "/auth/verify-email",
  "PasswordResetPath": "/auth/reset-password"
}
```

---

## Prompt 2

**User:**
Now add the IOptions<> to the UserController and fill out the url of the email verification email.

**Assistant:**
Updated `PocketAdvisor.WebApplication/Controllers/UserController.cs`:

- Added `IOptions<FrontendOptions> frontendOptions` parameter to the constructor (with null guard and assignment).
- Added `private IOptions<FrontendOptions> FrontendOptions { get; }` property.
- Replaced the `""` TODO in `CreateUserAsync` with:
```csharp
{ Url, $"{FrontendOptions.Value.BaseUrl}{FrontendOptions.Value.EmailVerificationPath}?token={result.Value}" }
```

---

## Prompt 3

**User:**
Move the string template into a private const field and use string.Format()

**Assistant:**
Updated `UserController.cs`:

- Added a new constant in the `#region Constants` block:
```csharp
/// <summary>
/// The format string used to build the email verification URL.
/// <c>{0}</c> is the frontend base URL, <c>{1}</c> is the email verification path, and <c>{2}</c> is the plain token.
/// </summary>
private const string EmailVerificationUrlFormat = "{0}{1}?token={2}";
```

- Replaced the interpolated string with:
```csharp
{ Url, string.Format(EmailVerificationUrlFormat, FrontendOptions.Value.BaseUrl, FrontendOptions.Value.EmailVerificationPath, result.Value) }
```
