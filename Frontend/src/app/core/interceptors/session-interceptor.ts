import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, finalize, Observable, shareReplay, switchMap, throwError } from 'rxjs';

import { SessionService } from '../services/session';

let refresh$: Observable<void> | null = null;

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (req.url.includes('/sessions/refresh')) {
    return next(req);
  }

  const jwt = sessionService.getJwt();
  const authReq = jwt ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${jwt}`) }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      const refreshToken = sessionService.getRefreshToken();
      if (!refreshToken) {
        sessionService.logout();
        router.navigate(['/auth/login']);
        return throwError(() => error);
      }

      refresh$ ??= sessionService.refresh({ refreshToken }).pipe(
        shareReplay(1),
        finalize(() => (refresh$ = null))
      );

      return refresh$.pipe(
        switchMap(() => {
          const retryReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${sessionService.getJwt()!}`)
          });
          return next(retryReq);
        }),
        catchError(refreshError => {
          sessionService.logout();
          router.navigate(['/auth/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
