import { Injectable, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/auth.model';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly storage = inject(TokenStorageService);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/auth`;

  /** Reactive auth state derived from token presence. */
  readonly isAuthenticated = computed(() => this.storage.accessToken() !== null);

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
