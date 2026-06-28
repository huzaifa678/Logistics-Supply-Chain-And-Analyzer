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
  phone: string;
}

export interface VerifyOtpRequest {
  email: string;
  code: string;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
}

/**
 * Login result. When `otpRequired` is true, `tokens` is null and the client must POST the
 * emailed/texted code to /verify-otp to finish signing in.
 */
export interface LoginResponse {
  otpRequired: boolean;
  tokens: AuthResponse | null;
}
