import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { ChartData } from 'chart.js';
import { ChartComponent } from '../../shared/chart.component';
import { AnalyticsStore } from '../../core/stores/analytics.store';
import { RoutesStore } from '../../core/stores/routes.store';

// Created, In transit, Delayed, Delivered, Cancelled
const STATUS_COLORS = ['#3b82f6', '#f59e0b', '#ef4444', '#22c55e', '#6b7280'];

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, DecimalPipe, ChartComponent],
  template: `
    <h1 class="mb-6 text-2xl font-semibold tracking-tight">Dashboard</h1>

    <!-- KPI tiles -->
    @if (kpis(); as k) {
      <div class="mb-6 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p class="text-xs uppercase tracking-wide text-gray-500">Total shipments</p>
          <p class="mt-1 text-2xl font-semibold">{{ k.total }}</p>
        </div>
        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p class="text-xs uppercase tracking-wide text-gray-500">In transit</p>
          <p class="mt-1 text-2xl font-semibold text-amber-600">{{ k.inTransit }}</p>
        </div>
        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p class="text-xs uppercase tracking-wide text-gray-500">Delayed</p>
          <p class="mt-1 text-2xl font-semibold text-red-600">{{ k.delayed }}</p>
        </div>
        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p class="text-xs uppercase tracking-wide text-gray-500">Delivered</p>
          <p class="mt-1 text-2xl font-semibold text-green-600">{{ k.delivered }}</p>
        </div>
        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <p class="text-xs uppercase tracking-wide text-gray-500">Routes</p>
          <p class="mt-1 text-2xl font-semibold">{{ routes().length }}</p>
        </div>
      </div>
    }

    @if (loading()) {
      <p class="text-sm text-gray-500">Loading analytics…</p>
    } @else {
      <div class="mb-6 grid gap-4 lg:grid-cols-2">
        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <h2 class="mb-3 font-medium">Shipments by status</h2>
          @if (statusChart(); as c) {
            <div class="h-64">
              <app-chart type="doughnut" [data]="c" />
            </div>
          } @else {
            <p class="text-sm text-gray-500">No shipment data yet.</p>
          }
        </div>

        <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
          <h2 class="mb-3 font-medium">Top warehouses by volume</h2>
          @if (warehouseChart(); as c) {
            <div class="h-64">
              <app-chart type="bar" [data]="c" [options]="barOptions" />
            </div>
          } @else {
            <p class="text-sm text-gray-500">No warehouse activity yet.</p>
          }
        </div>
      </div>
    }

    <!-- Network routes -->
    <div class="mb-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <h2 class="mb-3 font-medium">Network routes</h2>
      @if (routes().length === 0) {
        <p class="text-sm text-gray-500">No routes yet. Create routes from the Warehouses page.</p>
      } @else {
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="border-b border-gray-200 text-gray-500">
              <tr>
                <th class="px-3 py-2 font-medium">Origin → Destination</th>
                <th class="px-3 py-2 font-medium">Mode</th>
                <th class="px-3 py-2 font-medium">Distance (km)</th>
                <th class="px-3 py-2 font-medium">Cost</th>
              </tr>
            </thead>
            <tbody>
              @for (r of routes(); track r.id) {
                <tr class="border-b border-gray-100 last:border-0">
                  <td class="px-3 py-2">{{ r.originName }} → {{ r.destinationName }}</td>
                  <td class="px-3 py-2">{{ r.mode }}</td>
                  <td class="px-3 py-2">{{ r.distanceKm | number: '1.0-1' }}</td>
                  <td class="px-3 py-2">{{ r.cost | number: '1.2-2' }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

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
export class DashboardComponent {
  private readonly analyticsStore = inject(AnalyticsStore);
  private readonly routesStore = inject(RoutesStore);

  // Shared, cached state from the stores (single source of truth across views).
  protected readonly loading = this.analyticsStore.loading;
  protected readonly aggregates = this.analyticsStore.aggregates;
  protected readonly routes = this.routesStore.items;

  protected readonly barOptions = {
    responsive: true,
    maintainAspectRatio: false,
    scales: { x: { stacked: false }, y: { beginAtZero: true } },
  };

  protected readonly kpis = computed(() => {
    const d = this.aggregates();
    if (!d) return null;
    const by = (label: string) => d.byStatus.find((s) => s.status === label)?.count ?? 0;
    return {
      total: d.total,
      inTransit: by('In transit'),
      delayed: by('Delayed'),
      delivered: by('Delivered'),
    };
  });

  protected readonly statusChart = computed<ChartData<'doughnut'> | null>(() => {
    const d = this.aggregates();
    if (!d || d.total === 0) return null;
    return {
      labels: d.byStatus.map((s) => s.status),
      datasets: [{ data: d.byStatus.map((s) => s.count), backgroundColor: STATUS_COLORS }],
    };
  });

  protected readonly warehouseChart = computed<ChartData<'bar'> | null>(() => {
    const d = this.aggregates();
    if (!d || d.warehouses.length === 0) return null;
    const top = d.warehouses.slice(0, 8);
    return {
      labels: top.map((w) => w.warehouseId),
      datasets: [
        { label: 'Outbound', data: top.map((w) => w.outbound), backgroundColor: '#3b82f6' },
        { label: 'Inbound', data: top.map((w) => w.inbound), backgroundColor: '#22c55e' },
      ],
    };
  });

  constructor() {
    // Cached: fetched once, reused across navigation; reloaded by mutations elsewhere.
    this.analyticsStore.load();
    this.routesStore.load();
  }
}
