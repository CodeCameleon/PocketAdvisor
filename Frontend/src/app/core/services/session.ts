import { inject, Injectable } from '@angular/core';
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
