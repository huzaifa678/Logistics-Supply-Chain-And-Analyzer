import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';
import { Role } from '../models/auth.model';

/**
 * Route-level RBAC. Unauthenticated users go to login (with a return URL); authenticated users
 * lacking the required role are bounced to the dashboard. This is UX only — the API still
 * enforces authorization on every request.
 *
 * Usage:  { path: 'admin', canActivate: [roleGuard('Admin')], ... }
 */
export function roleGuard(...roles: Role[]): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
    }

    return auth.hasRole(...roles) ? true : router.createUrlTree(['/dashboard']);
  };
}
