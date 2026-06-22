import { RenderMode, ServerRoute } from '@angular/ssr';

/**
 * This is an authenticated SPA — pages render client-side (tokens live in the browser, and
 * protected data must not be fetched during a build-time prerender). The SSR server still serves
 * the app shell and Angular hydrates on the client.
 */
export const serverRoutes: ServerRoute[] = [
  {
    path: '**',
    renderMode: RenderMode.Client,
  },
];
