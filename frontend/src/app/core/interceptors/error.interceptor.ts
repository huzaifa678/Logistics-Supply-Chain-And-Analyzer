import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { TokenStorageService } from '../services/token-storage.service';

/**
 * Centralized HTTP error handling. On 401 (token missing/expired/revoked) it clears the session
 * and routes to login, so individual components don't repeat that logic.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const storage = inject(TokenStorageService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401 && !req.url.includes('/api/auth/')) {
        storage.clear();
        void router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
