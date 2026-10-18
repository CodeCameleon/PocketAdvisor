import { inject } from '@angular/core';
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';

import { SessionService } from '../services/session';
import { environment } from '../../../environments/environment';

/** The session endpoints authenticate with the refresh token cookie, not the JWT. */
const sessionsUrl = `${environment.apiUrl}/sessions/`;

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (req.url.startsWith(sessionsUrl)) {
    return next(req);
  }

  const jwt = sessionService.getJwt();
  const authReq = jwt ? req.clone({ headers: req.headers.set('Authorization', `Bearer ${jwt}`) }) : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) {
        return throwError(() => error);
      }

      return sessionService.refresh().pipe(
        switchMap(() => {
          const retryReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${sessionService.getJwt()!}`)
          });
          return next(retryReq);
        }),
        catchError(refreshError => {
          sessionService.clear();
          router.navigate(['/auth/login']);
          return throwError(() => refreshError);
        })
      );
    })
  );
};
