/**
 * App configuration. `apiBaseUrl` is left empty so the app calls the API with relative
 * paths (e.g. `/api/auth/login`). In dev, `proxy.conf.json` forwards `/api` to the backend;
 * in prod, an ingress / reverse proxy routes `/api` to the API service. This keeps the
 * frontend free of any hard-coded backend host.
 */
export const environment = {
  production: false,
  apiBaseUrl: '',
};
