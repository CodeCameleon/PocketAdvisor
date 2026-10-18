import { computed, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/sessions`;

  private readonly _jwt = signal<string | null>(null);

  /** The in-flight refresh request, shared by every concurrent caller. */
  private refresh$: Observable<void> | null = null;

  /** Signal that is true when a JWT is currently held in memory. */
  readonly isLoggedIn = computed(() => this._jwt() !== null);

  /** Restores the session from the refresh token cookie, if there is a valid one. Never fails. */
  restore(): Observable<void> {
    return this.refresh().pipe(catchError(() => of(void 0)));
  }

  /** Authenticates a user. The backend sets the refresh token cookie. */
  login(request: LoginRequest): Observable<void> {
    return this.http.post<LoginResponse>(`${this.base}/login`, request, { withCredentials: true }).pipe(
      tap(response => this._jwt.set(response.jsonWebToken)),
      map(() => void 0)
    );
  }

  /**
   * Rotates the refresh token cookie and stores the new JWT. The refresh token
   * is single-use, so concurrent callers share one request instead of racing.
   */
  refresh(): Observable<void> {
    this.refresh$ ??= this.http.post<LoginResponse>(`${this.base}/refresh`, null, { withCredentials: true }).pipe(
      tap(response => this._jwt.set(response.jsonWebToken)),
      map(() => void 0),
      finalize(() => (this.refresh$ = null)),
      shareReplay(1)
    );

    return this.refresh$;
  }

  /**
   * Revokes the refresh token and clears its cookie on the backend, then ends
   * the local session. The local session ends even if the backend is unreachable.
   */
  logout(): Observable<void> {
    return this.http.post<void>(`${this.base}/logout`, null, { withCredentials: true }).pipe(
      catchError(() => of(void 0)),
      map(() => this.clear())
    );
  }

  /** Ends the local session without contacting the backend. */
  clear(): void {
    this._jwt.set(null);
  }

  /** Returns the JWT held in memory, or null if not present. */
  getJwt(): string | null {
    return this._jwt();
  }
}
