import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { SessionService } from '../services/session';

/** Redirects non-administrators to the login page. */
export const adminGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  const jwt = sessionService.getJwt();

  if (jwt && isAdministrator(jwt)) {
    return true;
  }

  return router.createUrlTree(['/auth/login']);
};

function isAdministrator(jwt: string): boolean {
  try {
    const payload = JSON.parse(atob(jwt.split('.')[1]));
    const role: unknown = payload['role'] ?? payload['roles'] ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    if (Array.isArray(role)) {
      return role.includes('Administrator');
    }

    return role === 'Administrator';
  } catch {
    return false;
  }
}
