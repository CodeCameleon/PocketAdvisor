# Session Service & Interceptor Refresh Logic

**Date:** 2026-05-06

---

## Turn 1

### User

> I want you to update the Session service to not return the response outside the service.

### Assistant Thinking

Read `session.ts` to understand the current shape of `login` and `refresh`. Both returned `Observable<LoginResponse>` and used `tap` to store tokens. Read `login.ts` to confirm callers didn't actually consume the response value — `next: () => { ... }` with no argument used. Safe to change return type to `Observable<void>`.

### Assistant Response

Changed `login` and `refresh` in `SessionService` to return `Observable<void>` instead of `Observable<LoginResponse>`. Added `map(() => void 0)` after the `tap` to discard the response. Added `map` to the RxJS import. Updated JSDoc comments.

---

#### Modified: `Frontend/src/app/core/services/session.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { RefreshRequest } from '../models/refresh-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private static readonly JsonWebTokenKey = 'jsonWebToken';
  private static readonly RefreshTokenKey = 'refreshToken';

  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/sessions`;

  /** Authenticates a user and stores the returned tokens. */
  login(request: LoginRequest): Observable<void> {
    return this.http.post<LoginResponse>(`${this.base}/login`, request).pipe(
      tap(response => this.storeTokens(response)),
      map(() => void 0)
    );
  }

  /** Rotates the refresh token and stores the new tokens. */
  refresh(request: RefreshRequest): Observable<void> {
    return this.http.post<LoginResponse>(`${this.base}/refresh`, request).pipe(
      tap(response => this.storeTokens(response)),
      map(() => void 0)
    );
  }

  /** Removes the stored tokens, ending the local session. */
  logout(): void {
    localStorage.removeItem(SessionService.JsonWebTokenKey);
    localStorage.removeItem(SessionService.RefreshTokenKey);
  }

  /** Returns the stored json web token, or null if not present. */
  getJwt(): string | null {
    return localStorage.getItem(SessionService.JsonWebTokenKey);
  }

  /** Returns the stored refresh token, or null if not present. */
  getRefreshToken(): string | null {
    return localStorage.getItem(SessionService.RefreshTokenKey);
  }

  /** Returns true if a json web token is currently stored. */
  isLoggedIn(): boolean {
    return this.getJwt() !== null;
  }

  private storeTokens(response: LoginResponse): void {
    localStorage.setItem(SessionService.JsonWebTokenKey, response.jsonWebToken);
    localStorage.setItem(SessionService.RefreshTokenKey, response.refreshToken);
  }
}
```

---

### Design Decisions

- `map(() => void 0)` is used after `tap` to suppress the `LoginResponse` from leaking out of the service. The `tap` still runs first so tokens are stored before the observable completes.
- Callers in `login.ts` already used `next: () => { ... }` (no argument), so no call-site changes were needed.

---

## Turn 2

### User

> Where do you think the refresh token should be used to renew the jwt token?

### Assistant Thinking

Read the session interceptor to understand its current state — it only attached the JWT to outgoing requests, with no 401 handling. Considered where renewal logic belongs: the interceptor is the correct place as it sits on every HTTP request and can transparently retry after a token refresh. Identified the need to handle concurrent 401s with a shared observable.

### Assistant Response

The session interceptor is the right place. The pattern to implement:

1. Attach the JWT to outgoing requests as it already does.
2. Catch `401` responses with `catchError`.
3. On a 401, call `sessionService.refresh(...)` with the stored refresh token. If it succeeds, retry the original request — the `tap` inside `refresh` will have already stored the new JWT.
4. If the refresh itself fails, log the user out and redirect to login.
5. Handle concurrent requests: if several requests 401 at the same time, hold a shared `refresh$` observable using `shareReplay(1)` and reset it to `null` once done, so all retries share a single refresh call.

---

## Turn 3

### User

> Yes please

### Assistant Thinking

Read `app.routes.ts` to confirm the login path is `/auth/login`. Implemented the interceptor with: JWT attachment, 401 detection, refresh-token guard, shared `refresh$` with `shareReplay(1)`, retry with fresh JWT, and logout + redirect on refresh failure. Cleared `refresh$` manually in both `switchMap` and `catchError`.

### Assistant Response

---

#### Modified: `Frontend/src/app/core/interceptors/session-interceptor.ts`

```typescript
import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, Observable, shareReplay, switchMap, throwError } from 'rxjs';

import { SessionService } from '../services/session';

let refresh$: Observable<void> | null = null;

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  const jwt = sessionService.getJwt();
  const authReq = jwt
    ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${jwt}`) })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      const refreshToken = sessionService.getRefreshToken();
      if (!refreshToken) {
        sessionService.logout();
        router.navigate(['/auth/login']);
        return throwError(() => error);
      }

      // Share a single refresh call across all concurrent 401s.
      refresh$ ??= sessionService.refresh({ refreshToken }).pipe(
        shareReplay(1)
      );

      return refresh$.pipe(
        switchMap(() => {
          refresh$ = null;
          const retryReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${sessionService.getJwt()!}`)
          });
          return next(retryReq);
        }),
        catchError(refreshError => {
          refresh$ = null;
          sessionService.logout();
          router.navigate(['/auth/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
```

---

### Design Decisions

- `refresh$` is a module-level variable so it persists across interceptor invocations within the same app session, enabling deduplication of concurrent refresh calls.
- `shareReplay(1)` ensures all concurrent 401 retries subscribe to the same in-flight refresh observable rather than triggering multiple refresh requests.
- `refresh$` is reset to `null` in both the success (`switchMap`) and failure (`catchError`) branches so future 401s start a fresh refresh.

---

## Turn 4

### User

> I have made some changes to the interceptor. Can you check if it is good like this?

### Assistant Thinking

Read the updated interceptor. Two changes from the user: added `finalize(() => refresh$ = null)` directly on `refresh$` to clean it up in both completion and error cases (removing the need for manual `null` assignments in each branch), and added a bypass guard for requests to `/auth/refresh` to prevent infinite 401 → refresh → 401 loops.

### Assistant Response

The changes are a clear improvement:

**`finalize(() => refresh$ = null)`** — cleaner than manually clearing in each branch. `finalize` runs on both completion and error, removing the duplicated `refresh$ = null` lines and the risk of a forgotten assignment leaving a stale cached observable.

**Bypass for `/auth/refresh`** — important correctness fix. Without it, a 401 on the refresh call itself would trigger another refresh attempt, causing an infinite loop. The bypass breaks that cycle cleanly.

One thing to double-check: the bypass checks `req.url.includes('/auth/refresh')` but `SessionService` constructs the refresh URL as `${environment.apiUrl}/sessions/refresh`. If the actual path is `/sessions/refresh` rather than `/auth/refresh`, the guard won't trigger. The string in the `includes` check should match the real API path.
