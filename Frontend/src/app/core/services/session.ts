import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

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

  /** Authenticates a user, stores the returned tokens, and returns the response. */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  /** Rotates the refresh token, stores the new tokens, and returns the response. */
  refresh(request: RefreshRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/refresh`, request).pipe(
      tap(response => this.storeTokens(response))
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
