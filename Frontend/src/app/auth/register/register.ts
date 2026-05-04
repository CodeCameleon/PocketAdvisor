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
  selector: 'app-register',
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
  templateUrl: './register.html'
})
export class Register {
  private readonly fb = inject(FormBuilder);
  private readonly userService = inject(UserService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly form = this.fb.nonNullable.group({
    email: [''],
    password: [''],
    confirmPassword: [''],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');
  readonly successMessage = signal('');
  readonly hidePassword = signal(true);
  readonly hideConfirm = signal(true);

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.successMessage.set('');

    this.userService.createUser(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.successMessage.set('Account created! Please check your email to verify your address.');
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
