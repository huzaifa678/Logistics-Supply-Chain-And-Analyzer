import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { TokenStorageService } from '../services/token-storage.service';

/**
 * Attaches the bearer token to outgoing API requests. Skips the auth endpoints (login/register/
 * refresh/revoke), which must not carry a (possibly stale) access token.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(TokenStorageService).accessToken();
  const isAuthEndpoint = req.url.includes('/api/auth/');

  if (!token || isAuthEndpoint) {
    return next(req);
  }

  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
