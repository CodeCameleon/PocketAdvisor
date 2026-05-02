import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

import { SessionService } from '../services/session';

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const jwt = inject(SessionService).getJwt();

  if (!jwt) {
    return next(req);
  }

  return next(req.clone({
    headers: req.headers.set('Authorization', `Bearer ${jwt}`)
  }));
};
