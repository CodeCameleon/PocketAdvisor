# Refresh Token Cookie — Critical Audit Fix

**Date:** 2026-10-18

---

## Turn 1

### Prompt
> I want you to do the critical audit finding fix on a new feature branch.

### Thinking
The Critical finding in `AUDIT.md` is: **JWT + refresh token stored in `localStorage`** — XSS
exfiltration risk. The fix requires two coordinated halves:

**Backend:**
- Issue the refresh token as an `HttpOnly; Secure; SameSite=Strict` cookie (path `/api/sessions`)
  instead of returning it in the JSON body.
- Add a `POST /api/sessions/logout` endpoint: reads the cookie, hashes it, deletes it from the
  DB, clears the cookie unconditionally (so logged-out users aren't stuck), returns 204.
- Rotate the refresh token on each `/api/sessions/refresh` call (single-use tokens).
- New `SessionResponse` record (in `PocketAdvisor.Responses`) carries `JsonWebToken`,
  `[JsonIgnore] RefreshToken`, and `[JsonIgnore] RefreshTokenExpiryAt` so the service can
  return all three to the controller without serialising the token to the client.
- `LoginResponse` is stripped down to only `JsonWebToken`.
- New `CookieNames.cs` constant: `PocketAdvisor.RefreshToken`.
- Rate-limit logout under the existing `Refresh` policy (30 req/60 s) — same surface area.

**Frontend:**
- Replace `localStorage` JWT + refresh token with an in-memory JWT signal.
- All HTTP calls add `{ withCredentials: true }` so the browser sends the cookie.
- `SessionService.restore()` calls `refresh()` on startup (via `provideAppInitializer`) and
  swallows the error for logged-out visitors — one expected 400 in console on cold load.
- `refresh$` is a shared observable (`shareReplay(1)` + `finalize`) to serialise concurrent
  401 retries — no refresh storm.
- `SessionInterceptor` skips `/api/sessions/` URLs (login/logout/refresh themselves) to avoid
  loops; on 401 it calls `refresh()` then retries; on failure `clear()` + navigate to login.
- `navbar.logout()` calls `sessionService.logout()` (POST to backend) then navigates.
- `LegacyStorageKeys` and all `localStorage` references removed.

### Response

**New file:** `Backend/PocketAdvisor.Responses/Users/SessionResponse.cs`
```csharp
namespace PocketAdvisor.Responses.Users;

/// <summary>
/// Represents the full session data returned by the service layer after a successful
/// authentication or token-refresh operation.
/// <br />
/// The <see cref="RefreshToken"/> and <see cref="RefreshTokenExpiryAt"/> properties are
/// intentionally excluded from JSON serialisation — they must be sent to the client only
/// as an <c>HttpOnly</c> cookie, never in the response body.
/// </summary>
public record SessionResponse
{
    public required string JsonWebToken { get; init; }

    [JsonIgnore]
    public required string RefreshToken { get; init; }

    [JsonIgnore]
    public required DateTime RefreshTokenExpiryAt { get; init; }
}
```

**Updated:** `Backend/PocketAdvisor.Responses/Users/LoginResponse.cs` — `RefreshToken`
property removed; only `JsonWebToken` remains.

**Updated:** `Backend/PocketAdvisor.Services/Interfaces/IUserService.cs`
```csharp
Task<Result<SessionResponse>> LoginAsync(LoginRequest request);
Task<Result<SessionResponse>> RefreshAsync(RefreshRequest request);
Task<Result> LogoutAsync(RefreshRequest request);
```

**Updated:** `Backend/PocketAdvisor.Services/Implementations/UserService.cs`
- `LoginAsync` / `RefreshAsync` return `SessionResponse` (include `RefreshTokenExpiryAt`).
- `LogoutAsync`: validates token present → hashes with Refresh secret → deletes from DB if
  found → always returns `Result.Ok()` (idempotent).

**New file:** `Backend/PocketAdvisor.WebApplication/Constants/CookieNames.cs`
```csharp
namespace PocketAdvisor.WebApplication.Constants;

internal static class CookieNames
{
    internal const string RefreshToken = "PocketAdvisor.RefreshToken";
}
```

**Updated:** `Backend/PocketAdvisor.WebApplication/Controllers/SessionController.cs`
```csharp
[ApiController]
[Route(RouteTemplate)]
public class SessionController(IUserService userService) : BaseController
{
    private const string RouteTemplate = "api/sessions";
    private const string RefreshTokenCookiePath = "/" + RouteTemplate;

    private CookieOptions CreateRefreshTokenCookieOptions(DateTime expiresAt) => new()
    {
        Path = RefreshTokenCookiePath,
        Expires = expiresAt,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        IsEssential = true,
    };

    [HttpPost]
    [EnableRateLimiting(RateLimiterPolicies.Authentication)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await userService.LoginAsync(request);
        if (result.IsFailed) return HandleFailedResult(result);

        Response.Cookies.Append(
            CookieNames.RefreshToken,
            result.Value.RefreshToken,
            CreateRefreshTokenCookieOptions(result.Value.RefreshTokenExpiryAt));

        return StatusCode(StatusCodes.Status201Created,
            new LoginResponse { JsonWebToken = result.Value.JsonWebToken });
    }

    [HttpPost("logout")]
    [EnableRateLimiting(RateLimiterPolicies.Refresh)]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies[CookieNames.RefreshToken];
        await userService.LogoutAsync(new RefreshRequest { RefreshToken = token });

        Response.Cookies.Delete(CookieNames.RefreshToken,
            CreateRefreshTokenCookieOptions(DateTime.UtcNow));

        return NoContent();
    }

    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimiterPolicies.Refresh)]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies[CookieNames.RefreshToken];
        var result = await userService.RefreshAsync(new RefreshRequest { RefreshToken = token });

        if (result.IsFailed)
        {
            Response.Cookies.Delete(CookieNames.RefreshToken,
                CreateRefreshTokenCookieOptions(DateTime.UtcNow));
            return HandleFailedResult(result);
        }

        Response.Cookies.Append(
            CookieNames.RefreshToken,
            result.Value.RefreshToken,
            CreateRefreshTokenCookieOptions(result.Value.RefreshTokenExpiryAt));

        return StatusCode(StatusCodes.Status201Created,
            new LoginResponse { JsonWebToken = result.Value.JsonWebToken });
    }
}
```

**Updated:** `Frontend/src/app/core/services/session.ts`
```typescript
@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly _jwt = signal<string | null>(null);

  private refresh$: Observable<string> | null = null;

  restore(): Observable<void> {
    return this.refresh().pipe(catchError(() => of(void 0)));
  }

  login(request: LoginRequest): Observable<void> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/sessions`, request, { withCredentials: true })
      .pipe(tap(response => this._jwt.set(response.jsonWebToken)), map(() => void 0));
  }

  refresh(): Observable<string> {
    if (!this.refresh$) {
      this.refresh$ = this.http
        .post<LoginResponse>(`${environment.apiUrl}/sessions/refresh`, null, { withCredentials: true })
        .pipe(
          tap(response => this._jwt.set(response.jsonWebToken)),
          map(response => response.jsonWebToken),
          shareReplay(1),
          finalize(() => (this.refresh$ = null)),
        );
    }
    return this.refresh$;
  }

  logout(): Observable<void> {
    return this.http
      .post<void>(`${environment.apiUrl}/sessions/logout`, null, { withCredentials: true })
      .pipe(catchError(() => of(void 0)), tap(() => this.clear()), map(() => void 0));
  }

  clear(): void {
    this._jwt.set(null);
    this.refresh$ = null;
  }

  getJwt(): string | null {
    return this._jwt();
  }
}
```

**Updated:** `Frontend/src/app/core/interceptors/session-interceptor.ts`
```typescript
export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (req.url.includes(`${environment.apiUrl}/sessions/`)) {
    return next(req);
  }

  return next(req).pipe(
    catchError(error => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        return sessionService.refresh().pipe(
          switchMap(jwt => {
            const retried = req.clone({ setHeaders: { Authorization: `Bearer ${jwt}` } });
            return next(retried);
          }),
          catchError(() => {
            sessionService.clear();
            router.navigate(['/auth/login']);
            return throwError(() => error);
          }),
        );
      }
      return throwError(() => error);
    }),
  );
};
```

**Updated:** `Frontend/src/app/app.config.ts`
```typescript
provideAppInitializer(() => inject(SessionService).restore())
```

**Updated:** `Frontend/src/app/shared/navbar/navbar.ts`
```typescript
logout(): void {
  this.sessionService.logout().subscribe(() => this.router.navigate(['/auth/login']));
}
```

**Updated:** `Frontend/src/app/core/models/login-response.ts` — only `jsonWebToken`.
`Frontend/src/app/core/models/refresh-request.ts` — deleted (token now read from cookie server-side).

**Updated:** `Documentation/AUDIT.md` — Critical finding marked **Resolved**.
**Updated:** `Documentation/DOCUMENTATION.md` — session persistence section, guards, interceptor
behaviour, API table (login, logout, refresh), token lifecycle, rate-limit table, security list.

---

## Turn 2

### Prompt
> Wouldn't the Responses project be a better fit for the SessionTokens model with a different name?

### Thinking
`SessionTokens` was originally proposed in the `WebApplication` project. The user is correct:
model/record types that carry data across layer boundaries belong in `PocketAdvisor.Responses`,
consistent with `LoginResponse`, `AccountResponse`, etc. The name `SessionResponse` is more
idiomatic — it names what the service returns for a session operation, parallel to
`LoginResponse` from the controller layer.

### Response
Moved the record to `Backend/PocketAdvisor.Responses/Users/SessionResponse.cs` and renamed it
`SessionResponse`. Updated the service interface, service implementation, and controller to
use the new namespace and name. No functional change — only the location and identifier changed.

---

## Turn 3

### Prompt
> Create a commit message for the currently staged changes.

### Thinking
Staged files at this point: backend changes (SessionResponse, LoginResponse, RefreshRequest,
IUserService, UserService, CookieNames, SessionController). One focused sentence, past tense,
full stop, no bullet lists, per repo convention.

### Response
```
Moved the refresh token into an HttpOnly cookie on the backend.
```

---

## Turn 4

### Prompt
> There is no need for the LegacyStorageKeys to remain, you can safely delete all the old version things.

### Thinking
`LegacyStorageKeys` was a migration helper that read old `localStorage` keys and copied them
to new keys on app startup — a one-time migration shim from an earlier session design. Since
the entire `localStorage`-based session is being replaced by an in-memory JWT signal, the
shim is entirely dead code. Removed from `session.ts`; `app.config.ts` already only calls
`sessionService.restore()`.

### Response
Removed `LegacyStorageKeys` enum and the `migrateStorage()` call from `session.ts`. No
other files referenced the legacy keys.

---

## Turn 5

### Prompt
> Can you give me a commit message for the staged changes?

### Thinking
Staged at this point: frontend files (session.ts, session-interceptor.ts, app.config.ts,
navbar.ts, login-response.ts; refresh-request.ts deleted). One sentence, past tense, full stop.

### Response
```
Moved the frontend session to an in-memory JWT and the refresh token cookie.
```

---

## Turn 6

### Prompt
> I added them in separate commits. Can you give me a message for just the currently staged changes?

### Thinking
At this point only `Documentation/AUDIT.md` was staged — the security audit file updated to
mark the Critical finding as Resolved.

### Response
```
Updated the security audit findings.
```
