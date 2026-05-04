import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { SessionService } from '../../core/services/session';
import { ApiErrorService } from '../../core/services/api-error';
import { AuthCard } from '../auth-card/auth-card';

@Component({
  selector: 'app-login',
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
  templateUrl: './login.html'
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly sessionService = inject(SessionService);
  private readonly apiErrorService = inject(ApiErrorService);
  private readonly router = inject(Router);

  readonly form = this.fb.nonNullable.group({
    email: [''],
    password: [''],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly hidePassword = signal(true);

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.sessionService.login(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.router.navigate(['/dashboard']);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.apiErrorService.applyErrors(err, this.form));
      },
    });
  }

  togglePasswordVisibility(): void {
    this.hidePassword.update(v => !v);
  }
}
