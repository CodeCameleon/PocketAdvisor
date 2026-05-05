import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { SessionService } from '../services/session';

/** Redirects authenticated users to the accounts page. */
export const noAuthGuard: CanActivateFn = () => {
  const sessionService = inject(SessionService);
  const router = inject(Router);

  if (sessionService.isLoggedIn()) {
    return router.createUrlTree(['/accounts']);
  }

  return true;
};
