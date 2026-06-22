import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

const ACCESS_KEY = 'logistics.accessToken';
const REFRESH_KEY = 'logistics.refreshToken';

/**
 * Wraps token persistence. SSR-safe: on the server there is no `localStorage`, so every access
 * is guarded by `isPlatformBrowser`. The `accessToken` signal lets the rest of the app react to
 * login/logout without re-reading storage.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly accessToken = signal<string | null>(this.read(ACCESS_KEY));

  get refreshToken(): string | null {
    return this.read(REFRESH_KEY);
  }

  setTokens(accessToken: string, refreshToken: string): void {
    this.write(ACCESS_KEY, accessToken);
    this.write(REFRESH_KEY, refreshToken);
    this.accessToken.set(accessToken);
  }

  clear(): void {
    this.remove(ACCESS_KEY);
    this.remove(REFRESH_KEY);
    this.accessToken.set(null);
  }

  private read(key: string): string | null {
    return this.isBrowser ? localStorage.getItem(key) : null;
  }

  private write(key: string, value: string): void {
    if (this.isBrowser) localStorage.setItem(key, value);
  }

  private remove(key: string): void {
    if (this.isBrowser) localStorage.removeItem(key);
  }
}
