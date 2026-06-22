import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

/** Authenticated layout: top nav + routed content. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="min-h-dvh bg-gray-50 text-gray-900">
      <header class="border-b border-gray-200 bg-white">
        <nav class="mx-auto flex max-w-6xl items-center gap-6 px-4 py-3">
          <span class="text-lg font-semibold tracking-tight">Logistics Analyzer</span>
          <ul class="flex gap-4 text-sm">
            @for (link of links; track link.path) {
              <li>
                <a
                  [routerLink]="link.path"
                  routerLinkActive="text-blue-600 font-medium"
                  class="text-gray-600 hover:text-gray-900"
                  >{{ link.label }}</a
                >
              </li>
            }
          </ul>
          <button
            type="button"
            (click)="logout()"
            class="ml-auto rounded-md border border-gray-300 px-3 py-1.5 text-sm hover:bg-gray-100"
          >
            Sign out
          </button>
        </nav>
      </header>
      <main class="mx-auto max-w-6xl px-4 py-6">
        <router-outlet />
      </main>
    </div>
  `,
})
export class ShellComponent {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly links = [
    { path: 'dashboard', label: 'Dashboard' },
    { path: 'shipments', label: 'Shipments' },
    { path: 'routes', label: 'Routes' },
  ];

  protected logout(): void {
    this.auth.logout();
    void this.router.navigate(['/login']);
  }
}
