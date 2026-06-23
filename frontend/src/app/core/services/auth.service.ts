import { Injectable, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest, Role } from '../models/auth.model';
import { TokenStorageService } from './token-storage.service';

/**
 * The role claim key in the access token. The API stamps it via .NET's `ClaimTypes.Role`,
 * which serializes to this full URI (not the short "role"). We accept either, just in case.
 */
const ROLE_CLAIM = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

/** Best-effort decode of a JWT payload. Returns null for malformed/absent tokens (e.g. on SSR). */
function decodeJwtPayload(token: string | null): Record<string, unknown> | null {
  if (!token) return null;
  try {
    const segment = token.split('.')[1];
    if (!segment) return null;
    const json = atob(segment.replace(/-/g, '+').replace(/_/g, '/'));
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorageService);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/auth`;

  /** Reactive auth state derived from token presence. */
  readonly isAuthenticated = computed(() => this.storage.accessToken() !== null);

  /** The current user's role, decoded from the access-token claims (null when signed out). */
  readonly role = computed<Role | null>(() => {
    const claims = decodeJwtPayload(this.storage.accessToken());
    const raw = (claims?.[ROLE_CLAIM] ?? claims?.['role']) as string | undefined;
    return raw === 'Viewer' || raw === 'Operator' || raw === 'Admin' ? raw : null;
  });

  /** True when the signed-in user holds any of the given roles. UI gating only — the API is the real gate. */
  hasRole(...roles: Role[]): boolean {
    const current = this.role();
    return current !== null && roles.includes(current);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/login`, request)
      .pipe(tap((res) => this.storage.setTokens(res.accessToken, res.refreshToken)));
  }

  register(request: RegisterRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(`${this.baseUrl}/register`, request);
  }

  /** Rotating refresh — the API revokes the old token and returns a new pair. */
  refresh(): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.baseUrl}/refresh`, { refreshToken: this.storage.refreshToken })
      .pipe(tap((res) => this.storage.setTokens(res.accessToken, res.refreshToken)));
  }

  logout(): void {
    const refreshToken = this.storage.refreshToken;
    if (refreshToken) {
      // Best-effort server-side revoke; clear locally regardless of the outcome.
      this.http.post(`${this.baseUrl}/revoke`, { refreshToken }).subscribe({
        error: () => undefined,
      });
    }
    this.storage.clear();
  }
}
