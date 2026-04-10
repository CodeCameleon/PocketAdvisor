# PocketAdvisor

A smart personal finance web app that helps users manage budgets, track expenses and make informed financial decisions.

## Backend

The web application is built on ASP.NET Core and provides a RESTful API for the frontend.  
It uses SecureStore to manage sensitive configuration values, such as token secrets.

### Required secret keys

- `TokenSecrets:EmailVerification`
- `TokenSecrets:Jwt`
- `TokenSecrets:PasswordReset`
- `TokenSecrets:Refresh`

### CLI setup

Install the SecureStore CLI if you do not have it yet:

```powershell
dotnet tool install --global SecureStore.Client
```

Create the encrypted store and a key file:

```powershell
SecureStore create ./secrets/token-secrets.bin --keyfile ./secrets/token-secrets.key
```

Set token secrets:

```powershell
SecureStore --store ./secrets/token-secrets.bin --keyfile ./secrets/token-secrets.key set "TokenSecrets:EmailVerification" "replace-with-email-verification-secret"
SecureStore --store ./secrets/token-secrets.bin --keyfile ./secrets/token-secrets.key set "TokenSecrets:Jwt" "replace-with-jwt-secret"
SecureStore --store ./secrets/token-secrets.bin --keyfile ./secrets/token-secrets.key set "TokenSecrets:PasswordReset" "replace-with-password-reset-secret"
SecureStore --store ./secrets/token-secrets.bin --keyfile ./secrets/token-secrets.key set "TokenSecrets:Refresh" "replace-with-refresh-secret"
```
