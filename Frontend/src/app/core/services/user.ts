import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateUserRequest } from '../models/create-user-request';
import { ForgotPasswordRequest } from '../models/forgot-password-request';
import { ResetPasswordRequest } from '../models/reset-password-request';
import { VerifyEmailRequest } from '../models/verify-email-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/users`;

  /** Registers a new user. Triggers an email-verification email. */
  createUser(request: CreateUserRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  /** Sends a password-reset email to the given address. */
  forgotPassword(request: ForgotPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/forgot-password`, request);
  }

  /** Resets the user's password using the token from their reset email. */
  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/reset-password`, request);
  }

  /** Verifies the user's email address using the token from their verification email. */
  verifyEmail(request: VerifyEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/verify-email`, request);
  }
}
