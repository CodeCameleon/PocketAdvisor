# Navbar isLoggedIn Signal Fix

**Date:** 2026-05-07

---

## Turn 1

### User

> Failed to load resource: the server responded with a status of 400 (Bad Request)
> Failed to load resource: the server responded with a status of 401 (Unauthorized)
> ERROR RuntimeError: NG0100: ExpressionChangedAfterItHasBeenCheckedError: Expression has changed after it was
> checked. Previous value: '2'. Current value: '-1'. Expression location: _Navbar component.
>
> So this happened when I left the pc for like 30 minutes. I left the client and the server running. When I came
> back and tested the update dialog with a wrong name, this happened. What do you think caused this?

### Assistant Thinking

- Read `navbar.html`, `navbar.ts`, `session.ts`, `session-interceptor.ts`, and `update-account-name-dialog.ts`.
- The `NG0100` error originates in `Navbar` at the `@if (isLoggedIn)` block. The values `2`/`-1` are Angular's internal template flags for `@if` — not the boolean itself — indicating the condition flipped mid change-detection cycle.
- `isLoggedIn()` was a plain getter that read from `localStorage` on every call. If `logout()` was called asynchronously (e.g. from the interceptor) while Angular was mid-way through a change detection pass, the value of `isLoggedIn` could differ between the start and end of that pass, triggering NG0100.
- The JWT likely expired during the 30-minute idle period. The update dialog submitted with a bad name → 400 from validation. But a concurrent background request (e.g. accounts list refresh) may have hit a 401, causing the interceptor to call `logout()` mid-cycle.
- Root fix: replace the `localStorage`-reading getter with a proper Angular `Signal` so change detection tracks it as a stable reactive value.

### Assistant Response

The root cause is the JWT expiring while idle. Here's the chain:

1. 30 minutes away → JWT expired on the server.
2. Update dialog submitted with a wrong name → API returns **400** (validation error).
3. The interceptor ignores 400s — no refresh attempted, error rethrown.
4. A concurrent request (e.g. background accounts refresh) hits **401** → interceptor calls `logout()`, clearing localStorage.
5. `logout()` fires asynchronously while Angular is mid-change-detection on `Navbar`.
6. `isLoggedIn()` read from `localStorage` directly — its value changed between the start and end of the same CD cycle → **NG0100**.

The fix is to back `isLoggedIn` with an Angular `Signal` so the template reacts to a tracked value that can only change at CD boundaries.

---

## Turn 2

### User

> Can you do this fix?

### Assistant Thinking

- Three files need changing: `session.ts`, `navbar.ts`, `navbar.html`.
- After editing, grepped for all other callers of `isLoggedIn()` — found `auth.ts` and `no-auth.ts` guards. Since Angular signals are callable functions, `isLoggedIn()` still works in those files with no changes needed.
- Caught that `navbar.html` used `isLoggedIn` without `()` (was fine for a getter), updated both `@if` blocks to `isLoggedIn()`.

### Assistant Response

Three files changed:

---

#### Modified: `Frontend/src/app/core/services/session.ts`

```typescript
import { computed, inject, Injectable, signal } from '@angular/core';
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

  private readonly _jwt = signal<string | null>(
    localStorage.getItem(SessionService.JsonWebTokenKey)
  );

  /** Signal that is true when a JWT is currently stored. */
  readonly isLoggedIn = computed(() => this._jwt() !== null);

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
    this._jwt.set(null);
  }

  /** Returns the stored json web token, or null if not present. */
  getJwt(): string | null {
    return this._jwt();
  }

  /** Returns the stored refresh token, or null if not present. */
  getRefreshToken(): string | null {
    return localStorage.getItem(SessionService.RefreshTokenKey);
  }

  private storeTokens(response: LoginResponse): void {
    localStorage.setItem(SessionService.JsonWebTokenKey, response.jsonWebToken);
    localStorage.setItem(SessionService.RefreshTokenKey, response.refreshToken);
    this._jwt.set(response.jsonWebToken);
  }
}
```

---

#### Modified: `Frontend/src/app/shared/navbar/navbar.ts`

```typescript
export class Navbar {
  private readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);

  readonly isLoggedIn = this.sessionService.isLoggedIn;

  logout(): void {
    this.sessionService.logout();
    this.router.navigate(['/auth/login']);
  }
}
```

- Replaced the `get isLoggedIn(): boolean` getter with a signal reference forwarded from `SessionService`.

---

#### Modified: `Frontend/src/app/shared/navbar/navbar.html`

```html
<mat-toolbar class="navbar">
  <nav class="navbar-left">
    @if (isLoggedIn()) {
      <a mat-button routerLink="/accounts" routerLinkActive="nav-link-active">
        <mat-icon>account_balance_wallet</mat-icon>
        Accounts
      </a>
    }
  </nav>

  <span class="navbar-title">
    Pocket Advisor
  </span>

  <div class="navbar-right">
    @if (isLoggedIn()) {
      <button mat-button class="nav-auth-button" (click)="logout()">
        <mat-icon>logout</mat-icon>
        Logout
      </button>
    }
  </div>
</mat-toolbar>
```

- Updated both `@if` blocks from `isLoggedIn` to `isLoggedIn()` to correctly invoke the signal.

---

### Design Decisions

- `_jwt` is the single source of truth for the JWT in memory. `getJwt()` now reads from the signal instead of `localStorage` directly, keeping all reads consistent.
- `getRefreshToken()` still reads from `localStorage` directly — nothing needs to reactively track it, so a signal would add noise without benefit.
- The two route guards (`auth.ts`, `no-auth.ts`) call `isLoggedIn()` — this syntax works unchanged since Angular signals are callable functions.

---

## Turn 3

### User

> Export

### Assistant Thinking

- Retrieved the export format reference from memory.
- Wrote this file to `Prompts\navbar-isloggedin-signal-fix.md`.
