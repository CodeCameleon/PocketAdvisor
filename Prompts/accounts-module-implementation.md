# Accounts Module Implementation

**Date:** 2026-05-05

---

> Now I want you to implement the main page of the accounts module. It should list out the accounts of the currently logged in user as cards. The module is already created for you.

Explored the project structure to understand existing patterns (standalone components, signals, Angular Material, CSS variables), then implemented the accounts list page across four files.

**`accounts/account-list/account-list.ts`**
```typescript
import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AccountService } from '../../core/services/account';
import { AccountResponse } from '../../core/models/account-response';
import { CurrencyCode } from '../../core/enums/currency-code';

@Component({
  selector: 'app-account-list',
  imports: [
    CurrencyPipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './account-list.html',
  styleUrl: './account-list.css'
})
export class AccountList implements OnInit {
  private readonly accountService = inject(AccountService);

  readonly accounts = signal<AccountResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load accounts. Please try again.');
        this.loading.set(false);
      },
    });
  }

  /** Converts a numeric CurrencyCode enum value to its ISO 4217 alpha string (e.g. 840 → "USD"). */
  currencyAlpha(code: CurrencyCode): string {
    return CurrencyCode[code].toUpperCase();
  }
}
```

**`accounts/account-list/account-list.html`** — three states: spinner, error banner, empty state, and an `@for` card grid.

**`accounts/account-list/account-list.css`** — responsive auto-fill grid, cards styled using project CSS variables, matching existing shadow/border-radius language.

**`accounts/accounts-routing-module.ts`** — wired `AccountList` as the default `''` route.

---

> ---- Application did not stabilize within 9 seconds ----

The HTTP call in `ngOnInit` ran during SSR prerendering (all routes were `RenderMode.Prerender`) and never resolved. Fixed by adding a `RenderMode.Client` entry for `accounts/**` in `app.routes.server.ts`.

**`app.routes.server.ts`**
```typescript
import { RenderMode, ServerRoute } from '@angular/ssr';

export const serverRoutes: ServerRoute[] = [
  { path: 'accounts/**', renderMode: RenderMode.Client },
  { path: '**', renderMode: RenderMode.Prerender }
];
```

---

> Why did you put the AccountList into the imports of the module?

Removed the unnecessary `AccountList` import from `AccountsModule` — standalone components wired through the router don't need to be declared in the module, consistent with how auth components are handled.

---

> Next, I want you to make it so when the login is successful, the user is redirected to the accounts page.

Changed the post-login navigation in `login.ts` from `/dashboard` to `/accounts`.

---

> I don't want the login page to flash when the redirect happens. Could you use something like Angular guards?

Created `core/guards/guest.ts` (a `CanActivateFn` that redirects logged-in users to `/accounts`), applied it to the login route, and removed the `afterNextRender` redirect from the component.

---

> That name is a bit misleading, what about authGuard? ... Let's go with authGuard, and make it so it is "you must be authenticated to access this route." And we are going to use it on the accounts module instead and redirect to the login.

Renamed and inverted the guard to `authGuard`, protecting `/accounts` instead. Removed it from the login route.

**`core/guards/auth.ts`**
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session';

/** Redirects unauthenticated users to the login page. */
export const authGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (sessionService.isLoggedIn()) return true;

  return router.createUrlTree(['/auth/login']);
};
```

**`app.routes.ts`** — added `canActivate: [authGuard]` to the `accounts` route.

---

> Nice, but I think I also want the opposite of this guard for the auth module.

Created `core/guards/no-auth.ts` — mirrors `authGuard`, redirecting logged-in users to `/accounts`. Applied to the `auth` route in `app.routes.ts`.

**`core/guards/no-auth.ts`**
```typescript
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from '../services/session';

/** Redirects authenticated users to the accounts page. */
export const noAuthGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (sessionService.isLoggedIn()) return router.createUrlTree(['/accounts']);

  return true;
};
```

---

> ERROR ReferenceError: localStorage is not defined

`SessionService` accessed `localStorage` directly, which throws on the server. Fixed by injecting `PLATFORM_ID` and guarding every `localStorage` access with `isPlatformBrowser`.

**`core/services/session.ts`** — added `isPlatformBrowser` checks to `getJwt`, `getRefreshToken`, `logout`, and `storeTokens`. On the server all return `null`/no-op, so `isLoggedIn()` safely returns `false` during SSR.

---

> It works now, but I still get some flashing when trying to open the login page when logged in.

`noAuthGuard` ran during SSR prerendering, saw `isLoggedIn() = false` (no `localStorage` server-side), allowed the auth module to render, then the browser re-ran the guard and redirected — causing a flash. Fixed by adding `auth/**` to `RenderMode.Client` in `app.routes.server.ts`.

**`app.routes.server.ts`**
```typescript
export const serverRoutes: ServerRoute[] = [
  { path: 'accounts/**', renderMode: RenderMode.Client },
  { path: 'auth/**', renderMode: RenderMode.Client },
  { path: '**', renderMode: RenderMode.Prerender }
];
```
