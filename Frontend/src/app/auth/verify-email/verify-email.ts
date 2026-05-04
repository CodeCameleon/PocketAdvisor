import { afterNextRender, Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { UserService } from '../../core/services/user';
import { AuthCard } from '../auth-card/auth-card';

@Component({
  selector: 'app-verify-email',
  imports: [
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    AuthCard
  ],
  templateUrl: './verify-email.html',
  styleUrl: './verify-email.css'
})
export class VerifyEmail {
  private readonly route = inject(ActivatedRoute);
  private readonly userService = inject(UserService);

  readonly loading = signal(true);
  readonly success = signal(false);
  readonly errorMessage = signal('');

  constructor() {
    afterNextRender(() => {
      const token = decodeURIComponent(this.route.snapshot.queryParamMap.get('token') ?? '');

      this.userService.verifyEmail({ token }).subscribe({
        next: () => {
          this.loading.set(false);
          this.success.set(true);
        },
        error: (err: HttpErrorResponse) => {
          this.loading.set(false);
          const errors = err.error?.errors as Record<string, string[]> | undefined;
          const message = errors ? Object.values(errors).flat()[0] : undefined;
          this.errorMessage.set(message ?? 'An unexpected error occurred. Please try again.');
        },
      });
    });
  }
}
