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
    AuthCard
  ],
  templateUrl: './reset-password.html'
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
