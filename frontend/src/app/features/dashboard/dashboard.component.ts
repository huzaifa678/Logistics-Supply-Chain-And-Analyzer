import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  template: `
    <h1 class="mb-6 text-2xl font-semibold tracking-tight">Dashboard</h1>
    <div class="grid gap-4 sm:grid-cols-2">
      <a
        routerLink="/shipments"
        class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm hover:border-blue-300"
      >
        <h2 class="font-medium">Shipments</h2>
        <p class="mt-1 text-sm text-gray-600">Browse shipments by status and inspect risk.</p>
      </a>
      <a
        routerLink="/routes"
        class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm hover:border-blue-300"
      >
        <h2 class="font-medium">Route estimate</h2>
        <p class="mt-1 text-sm text-gray-600">Estimate duration and cost between warehouses.</p>
      </a>
    </div>
  `,
})
export class DashboardComponent {}
