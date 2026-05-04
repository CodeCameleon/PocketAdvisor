# Reset Password Component

**Date:** 2026-05-04

---

## Turn 1

### User

> I want you to implement the reset password component. It is already created for you to work with.
> You can use the email verification for reference, because it is similar.

### Assistant Thinking

Explored the project structure to locate the existing reset-password scaffold and the reference components. Key files read:

- `auth/verify-email/verify-email.ts` and `.html` — the primary reference; reads a token from the query string inside `afterNextRender`, calls a service method, and reacts via `loading`, `success`, and `errorMessage` signals.
- `auth/forgot-password/forgot-password.ts` and `.html` — form-based auth pattern using `FormBuilder`, `ApiErrorService`, and a `submitted` signal to swap to a success banner.
- `auth/register/register.ts` and `.html` — password fields with show/hide toggles using `hidePassword`/`hideConfirm` signals and `togglePassword()`/`toggleConfirm()` methods.
- `core/services/user.ts` — confirmed `resetPassword(request: ResetPasswordRequest): Observable<void>` exists, posting to `/users/reset-password`.
- `core/models/reset-password-request.ts` — interface has `token`, `password`, `confirmPassword`.
- `core/services/api-error.ts` — `applyErrors(err, form)` maps server validation errors onto form controls and returns a global error string.
- `reset-password/reset-password.css` — empty; all required styles come from shared auth card globals.

Decided to combine the `verify-email` token-reading pattern with the `register` password-field pattern and the `forgot-password` form submission pattern.

### Assistant Response

#### Modified: `Frontend/src/app/auth/reset-password/reset-password.ts`

```typescript
import { afterNextRender, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { UserService } from '../../core/services/user';
import { ApiErrorService } from '../../core/services/api-error';
import { AuthCard } from '../auth-card/auth-card';

@Component({
  selector: 'app-reset-password',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    AuthCard,
  ],
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword {
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly apiErrorService = inject(ApiErrorService);

  private token = '';

  readonly form = this.fb.nonNullable.group({
    password: [''],
    confirmPassword: [''],
  });

  readonly loading = signal(false);
  readonly success = signal(false);
  readonly errorMessage = signal('');
  readonly hidePassword = signal(true);
  readonly hideConfirm = signal(true);

  constructor() {
    afterNextRender(() => {
      this.token = decodeURIComponent(this.route.snapshot.queryParamMap.get('token') ?? '');
    });
  }

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    const { password, confirmPassword } = this.form.getRawValue();

    this.userService.resetPassword({ token: this.token, password, confirmPassword }).subscribe({
      next: () => {
        this.loading.set(false);
        this.success.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.apiErrorService.applyErrors(err, this.form));
      },
    });
  }

  togglePassword(): void {
    this.hidePassword.update(v => !v);
  }

  toggleConfirm(): void {
    this.hideConfirm.update(v => !v);
  }
}
```

---

#### Modified: `Frontend/src/app/auth/reset-password/reset-password.html`

```html
<app-auth-card subtitle="Set a new password">
  @if (success()) {
    <div class="success-banner">
      <mat-icon>lock_reset</mat-icon>

      <span>
        Your password has been reset. You can now sign in with your new password.
      </span>
    </div>
  } @else {
    <form [formGroup]="form" (ngSubmit)="onSubmit()" novalidate class="auth-form">
      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="full-width">
        <mat-label>New Password</mat-label>

        <input matInput [type]="hidePassword() ? 'password' : 'text'" formControlName="password" autocomplete="new-password" />

        <button mat-icon-button matSuffix type="button" (click)="togglePassword()" [attr.aria-label]="hidePassword() ? 'Show password' : 'Hide password'">
          <mat-icon>{{hidePassword() ? 'visibility_off' : 'visibility'}}</mat-icon>
        </button>

        @if (form.controls.password.hasError('serverError')) {
          <mat-error>
            {{form.controls.password.getError('serverError')}}
          </mat-error>
        }
      </mat-form-field>

      <mat-form-field appearance="outline" subscriptSizing="dynamic" class="full-width">
        <mat-label>Confirm Password</mat-label>

        <input matInput [type]="hideConfirm() ? 'password' : 'text'" formControlName="confirmPassword" autocomplete="new-password" />

        <button mat-icon-button matSuffix type="button" (click)="toggleConfirm()" [attr.aria-label]="hideConfirm() ? 'Show password' : 'Hide password'">
          <mat-icon>{{hideConfirm() ? 'visibility_off' : 'visibility'}}</mat-icon>
        </button>

        @if (form.controls.confirmPassword.hasError('serverError')) {
          <mat-error>
            {{form.controls.confirmPassword.getError('serverError')}}
          </mat-error>
        }
      </mat-form-field>

      @if (errorMessage()) {
        <p class="error-message">
          {{errorMessage()}}
        </p>
      }

      <button mat-flat-button class="submit-button" type="submit" [disabled]="loading()">
        @if (loading()) {
          <mat-spinner diameter="20" />
        } @else {
          Reset Password
        }
      </button>
    </form>
  }

  <div class="auth-links">
    <a routerLink="/auth/login" class="auth-link">
      Back to sign in
    </a>
  </div>
</app-auth-card>
```

---

### Design Decisions

- **`afterNextRender` for token extraction** — mirrors `verify-email` to ensure the query param is read after hydration, keeping the component SSR-safe.
- **Token stored as a private field, not a form control** — the token is not user-editable input; keeping it out of the form group is cleaner and prevents it from being accidentally cleared or validated alongside the password fields.
- **No component-level CSS added** — all necessary styles (`.auth-form`, `.submit-button`, `.error-message`, `.success-banner`, `.auth-links`, `.auth-link`) are defined in the shared auth card stylesheet, so the component CSS file remains empty.
- **`success` signal instead of redirect** — consistent with `verify-email`; shows an in-place success banner rather than navigating away, giving the user confirmation before they choose to go to the login page.

---

## Turn 2

### User

> Export

### Assistant Thinking

User triggered the Export macro. Wrote this conversation to `Prompts\reset-password-component.md` following the canonical export format from memory.

### Assistant Response

Export saved.

---
