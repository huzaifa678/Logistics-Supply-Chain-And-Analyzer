import { Component, computed, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ChartData } from 'chart.js';
import { ChartComponent } from '../../shared/chart.component';
import { AuthService } from '../../core/services/auth.service';
import { WarehouseService } from '../../core/services/warehouse.service';
import { RouteService } from '../../core/services/route.service';
import { WarehousesStore } from '../../core/stores/warehouses.store';
import { RoutesStore } from '../../core/stores/routes.store';
import { AnalyticsStore } from '../../core/stores/analytics.store';
import { TransportMode } from '../../core/models/shipment.model';

@Component({
  selector: 'app-warehouse-overview',
  imports: [ReactiveFormsModule, ChartComponent],
  template: `
    <div class="mb-6">
      <h1 class="text-2xl font-semibold tracking-tight">Warehouses</h1>
      <p class="mt-1 text-sm text-gray-600">Manage warehouses and routes, and view throughput.</p>
    </div>

    <!-- Management (Operator/Admin only) -->
    @if (canManage()) {
      <div class="mb-6 grid gap-4 lg:grid-cols-2">
        <!-- Create warehouse -->
        <form
          [formGroup]="warehouseForm"
          (ngSubmit)="createWarehouse()"
          class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm"
        >
          <h2 class="mb-3 font-medium">Create warehouse</h2>
          <div class="grid gap-3 sm:grid-cols-2">
            <label class="block text-sm sm:col-span-2">
              <span class="text-gray-600">Name</span>
              <input
                formControlName="name"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
            <label class="block text-sm">
              <span class="text-gray-600">Latitude</span>
              <input
                type="number"
                formControlName="latitude"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
            <label class="block text-sm">
              <span class="text-gray-600">Longitude</span>
              <input
                type="number"
                formControlName="longitude"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
            <label class="block text-sm">
              <span class="text-gray-600">Capacity units</span>
              <input
                type="number"
                formControlName="capacityUnits"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
          </div>
          <button
            type="submit"
            [disabled]="warehouseForm.invalid || savingWarehouse()"
            class="mt-3 rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {{ savingWarehouse() ? 'Saving…' : 'Create warehouse' }}
          </button>
          @if (warehouseMsg(); as m) {
            <p class="mt-2 text-sm" [class.text-green-600]="m.ok" [class.text-red-600]="!m.ok">
              {{ m.text }}
            </p>
          }
        </form>

        <!-- Create route -->
        <form
          [formGroup]="routeForm"
          (ngSubmit)="createRoute()"
          class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm"
        >
          <h2 class="mb-3 font-medium">Create route</h2>
          @if (registered().length < 2) {
            <p class="text-sm text-gray-500">Create at least two warehouses first.</p>
          } @else {
            <div class="grid gap-3 sm:grid-cols-2">
              <label class="block text-sm">
                <span class="text-gray-600">Origin</span>
                <select
                  formControlName="originWarehouseId"
                  class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  @for (w of registered(); track w.id) {
                    <option [value]="w.id">{{ w.name || w.id }}</option>
                  }
                </select>
              </label>
              <label class="block text-sm">
                <span class="text-gray-600">Destination</span>
                <select
                  formControlName="destinationWarehouseId"
                  class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  @for (w of registered(); track w.id) {
                    <option [value]="w.id">{{ w.name || w.id }}</option>
                  }
                </select>
              </label>
              <label class="block text-sm">
                <span class="text-gray-600">Distance (km)</span>
                <input
                  type="number"
                  formControlName="distanceKm"
                  class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </label>
              <label class="block text-sm">
                <span class="text-gray-600">Cost</span>
                <input
                  type="number"
                  formControlName="cost"
                  class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
                />
              </label>
              <label class="block text-sm sm:col-span-2">
                <span class="text-gray-600">Mode</span>
                <select
                  formControlName="mode"
                  class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
                >
                  @for (m of modes; track m.value) {
                    <option [ngValue]="m.value">{{ m.label }}</option>
                  }
                </select>
              </label>
            </div>
            <button
              type="submit"
              [disabled]="routeForm.invalid || savingRoute()"
              class="mt-3 rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {{ savingRoute() ? 'Saving…' : 'Create route' }}
            </button>
          }
          @if (routeMsg(); as m) {
            <p class="mt-2 text-sm" [class.text-green-600]="m.ok" [class.text-red-600]="!m.ok">
              {{ m.text }}
            </p>
          }
        </form>
      </div>
    }

    <!-- Registered warehouses -->
    <div class="mb-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <h2 class="mb-3 font-medium">Registered warehouses</h2>
      @if (registered().length === 0) {
        <p class="text-sm text-gray-500">No warehouses yet.</p>
      } @else {
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="border-b border-gray-200 text-gray-500">
              <tr>
                <th class="px-3 py-2 font-medium">Name</th>
                <th class="px-3 py-2 font-medium">Id</th>
                <th class="px-3 py-2 font-medium">Location</th>
                <th class="px-3 py-2 font-medium">Capacity</th>
              </tr>
            </thead>
            <tbody>
              @for (w of registered(); track w.id) {
                <tr class="border-b border-gray-100 last:border-0">
                  <td class="px-3 py-2 font-medium">{{ w.name || '—' }}</td>
                  <td class="px-3 py-2 font-mono text-xs">{{ w.id }}</td>
                  <td class="px-3 py-2">{{ w.latitude }}, {{ w.longitude }}</td>
                  <td class="px-3 py-2">{{ w.capacityUnits }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>

    <!-- Throughput (derived from shipments) -->
    <div class="rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
      <h2 class="mb-3 font-medium">Volume by warehouse</h2>
      <p class="mb-3 text-xs text-gray-500">Derived from shipment origins (outbound) and destinations (inbound).</p>
      @if (chart(); as c) {
        <div class="h-80">
          <app-chart type="bar" [data]="c" [options]="options" />
        </div>
      } @else {
        <p class="text-sm text-gray-500">No shipment activity yet.</p>
      }
    </div>
  `,
})
export class WarehouseOverviewComponent {
  private readonly warehousesStore = inject(WarehousesStore);
  private readonly routesStore = inject(RoutesStore);
  private readonly analyticsStore = inject(AnalyticsStore);
  private readonly warehouseService = inject(WarehouseService);
  private readonly routeService = inject(RouteService);
  private readonly auth = inject(AuthService);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly canManage = computed(() => this.auth.hasRole('Operator', 'Admin'));

  // Read state from the shared stores (single source of truth).
  protected readonly registered = this.warehousesStore.items;
  protected readonly aggregates = this.analyticsStore.aggregates;

  protected readonly savingWarehouse = signal(false);
  protected readonly savingRoute = signal(false);
  protected readonly warehouseMsg = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly routeMsg = signal<{ ok: boolean; text: string } | null>(null);

  protected readonly modes = [
    { value: TransportMode.Road, label: 'Road' },
    { value: TransportMode.Rail, label: 'Rail' },
    { value: TransportMode.Sea, label: 'Sea' },
    { value: TransportMode.Air, label: 'Air' },
    { value: TransportMode.Intermodal, label: 'Intermodal' },
  ];

  protected readonly warehouseForm = this.fb.group({
    name: ['', [Validators.required]],
    latitude: [0, [Validators.required, Validators.min(-90), Validators.max(90)]],
    longitude: [0, [Validators.required, Validators.min(-180), Validators.max(180)]],
    capacityUnits: [0, [Validators.required, Validators.min(0)]],
  });

  protected readonly routeForm = this.fb.group({
    originWarehouseId: ['', [Validators.required]],
    destinationWarehouseId: ['', [Validators.required]],
    distanceKm: [1, [Validators.required, Validators.min(0.1)]],
    cost: [0, [Validators.required, Validators.min(0)]],
    mode: [TransportMode.Road, [Validators.required]],
  });

  protected readonly options = {
    responsive: true,
    maintainAspectRatio: false,
    scales: { x: { stacked: true }, y: { stacked: true, beginAtZero: true } },
  };

  protected readonly chart = computed<ChartData<'bar'> | null>(() => {
    const list = this.aggregates()?.warehouses ?? [];
    if (list.length === 0) return null;
    const top = list.slice(0, 12);
    return {
      labels: top.map((w) => w.warehouseId),
      datasets: [
        { label: 'Outbound', data: top.map((w) => w.outbound), backgroundColor: '#3b82f6' },
        { label: 'Inbound', data: top.map((w) => w.inbound), backgroundColor: '#22c55e' },
      ],
    };
  });

  constructor() {
    this.warehousesStore.load();
    this.analyticsStore.load();
  }

  protected createWarehouse(): void {
    if (this.warehouseForm.invalid) return;
    this.savingWarehouse.set(true);
    this.warehouseMsg.set(null);
    this.warehouseService.create(this.warehouseForm.getRawValue()).subscribe({
      next: () => {
        this.savingWarehouse.set(false);
        this.warehouseMsg.set({ ok: true, text: 'Warehouse created.' });
        this.warehouseForm.reset();
        // Refresh the shared store so dropdowns everywhere pick up the new warehouse.
        this.warehousesStore.reload();
      },
      error: (err) => {
        this.savingWarehouse.set(false);
        this.warehouseMsg.set({ ok: false, text: err.error?.error ?? err.error ?? 'Failed to create warehouse.' });
      },
    });
  }

  protected createRoute(): void {
    if (this.routeForm.invalid) return;
    const v = this.routeForm.getRawValue();
    if (v.originWarehouseId === v.destinationWarehouseId) {
      this.routeMsg.set({ ok: false, text: 'Origin and destination must differ.' });
      return;
    }
    this.savingRoute.set(true);
    this.routeMsg.set(null);
    this.routeService.create(v).subscribe({
      next: () => {
        this.savingRoute.set(false);
        this.routeMsg.set({ ok: true, text: 'Route created.' });
        // Refresh the shared routes store so the dashboard reflects it.
        this.routesStore.reload();
      },
      error: (err) => {
        this.savingRoute.set(false);
        this.routeMsg.set({ ok: false, text: err.error?.error ?? err.error ?? 'Failed to create route.' });
      },
    });
  }
}
