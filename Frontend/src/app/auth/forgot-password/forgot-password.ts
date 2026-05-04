import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { UserService } from '../../core/services/user';
import { ApiErrorService } from '../../core/services/api-error';
import { AuthCard } from '../auth-card/auth-card';

@Component({
  selector: 'app-forgot-password',
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
  templateUrl: './forgot-password.html'
})
export class ForgotPassword {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly form = this.fb.nonNullable.group({
    email: [''],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly submitted = signal(false);

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.userService.forgotPassword(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.submitted.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.apiErrorService.applyErrors(err, this.form));
      },
    });
  }
}
