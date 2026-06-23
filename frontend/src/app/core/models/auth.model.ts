/** Mirrors the backend Role enum (Logistics.Domain.Identity.Role), serialized as its name. */
export type Role = 'Viewer' | 'Operator' | 'Admin';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}
