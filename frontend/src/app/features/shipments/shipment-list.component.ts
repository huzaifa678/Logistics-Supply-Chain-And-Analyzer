import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule, NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ChartData } from 'chart.js';
import { ShipmentService } from '../../core/services/shipment.service';
import { AuthService } from '../../core/services/auth.service';
import { AnalyticsStore } from '../../core/stores/analytics.store';
import { WarehousesStore } from '../../core/stores/warehouses.store';
import { ChartComponent } from '../../shared/chart.component';
import { Shipment, ShipmentStatus, TransportMode } from '../../core/models/shipment.model';

type Transition = 'Dispatch' | 'Delay' | 'Deliver' | 'Cancel';

const STATUS_COLORS = ['#3b82f6', '#f59e0b', '#ef4444', '#22c55e', '#6b7280'];

@Component({
  selector: 'app-shipment-list',
  imports: [FormsModule, ReactiveFormsModule, DatePipe, ChartComponent],
  template: `
    <div class="mb-6 flex items-end justify-between gap-4">
      <h1 class="text-2xl font-semibold tracking-tight">Shipments</h1>
      <label class="text-sm">
        <span class="mr-2 text-gray-600">Status</span>
        <select
          [(ngModel)]="status"
          (ngModelChange)="load()"
          class="rounded-md border border-gray-300 px-3 py-1.5"
        >
          @for (option of statuses; track option.value) {
            <option [ngValue]="option.value">{{ option.label }}</option>
          }
        </select>
      </label>
    </div>

    <!-- Create shipment (Operator/Admin only) -->
    @if (canManage()) {
      <form
        [formGroup]="createForm"
        (ngSubmit)="createShipment()"
        class="mb-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm"
      >
        <h2 class="mb-3 font-medium">Create shipment</h2>
        @if (warehouses().length < 2) {
          <p class="text-sm text-gray-500">Add at least two warehouses first (Warehouses page).</p>
        } @else {
          <div class="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
            <label class="block text-sm">
              <span class="text-gray-600">Tracking number</span>
              <input
                formControlName="trackingNumber"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
            <label class="block text-sm">
              <span class="text-gray-600">Origin</span>
              <select
                formControlName="originWarehouseId"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              >
                <option value="" disabled>Select…</option>
                @for (w of warehouses(); track w.id) {
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
                <option value="" disabled>Select…</option>
                @for (w of warehouses(); track w.id) {
                  <option [value]="w.id">{{ w.name || w.id }}</option>
                }
              </select>
            </label>
            <label class="block text-sm">
              <span class="text-gray-600">Customer phone</span>
              <input
                formControlName="customerPhone"
                placeholder="+15551234567"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
            <label class="block text-sm">
              <span class="text-gray-600">Weight (kg)</span>
              <input
                type="number"
                formControlName="weightKg"
                class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
              />
            </label>
            <label class="block text-sm">
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
            [disabled]="createForm.invalid || creating()"
            class="mt-3 rounded-md bg-blue-600 px-4 py-2 text-sm text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {{ creating() ? 'Creating…' : 'Create shipment' }}
          </button>
        }
        @if (createMsg(); as m) {
          <p class="mt-2 text-sm" [class.text-green-600]="m.ok" [class.text-red-600]="!m.ok">
            {{ m.text }}
          </p>
        }
      </form>
    }

    @if (statusChart(); as c) {
      <div class="mb-6 rounded-xl border border-gray-200 bg-white p-5 shadow-sm">
        <h2 class="mb-3 font-medium">Status breakdown</h2>
        <div class="h-56">
          <app-chart type="bar" [data]="c" [options]="chartOptions" />
        </div>
      </div>
    }

    @if (loading()) {
      <p class="text-sm text-gray-500">Loading…</p>
    } @else if (error()) {
      <p class="text-sm text-red-600">{{ error() }}</p>
    } @else if (shipments().length === 0) {
      <p class="text-sm text-gray-500">No shipments with this status.</p>
    } @else {
      <div class="overflow-x-auto rounded-xl border border-gray-200 bg-white">
        <table class="w-full text-left text-sm">
          <thead class="border-b border-gray-200 text-gray-500">
            <tr>
              <th class="px-4 py-2 font-medium">Tracking</th>
              <th class="px-4 py-2 font-medium">Origin → Dest</th>
              <th class="px-4 py-2 font-medium">Mode</th>
              <th class="px-4 py-2 font-medium">Weight (kg)</th>
              <th class="px-4 py-2 font-medium">Created</th>
              <!-- Operator/Admin-only column (the API also enforces this on POST /status). -->
              @if (canManage()) {
                <th class="px-4 py-2 font-medium">Actions</th>
              }
            </tr>
          </thead>
          <tbody>
            @for (s of shipments(); track s.id) {
              <tr class="border-b border-gray-100 last:border-0">
                <td class="px-4 py-2 font-mono">{{ s.trackingNumber }}</td>
                <td class="px-4 py-2">{{ s.originWarehouseId }} → {{ s.destinationWarehouseId }}</td>
                <td class="px-4 py-2">{{ s.mode }}</td>
                <td class="px-4 py-2">{{ s.weightKg }}</td>
                <td class="px-4 py-2">{{ s.createdAt | date: 'short' }}</td>
                @if (canManage()) {
                  <td class="px-4 py-2">
                    <div class="flex gap-2">
                      @for (t of transitionsFor(s.status); track t) {
                        <button
                          type="button"
                          (click)="act(s, t)"
                          [disabled]="acting()"
                          class="rounded-md border border-gray-300 px-2 py-1 text-xs hover:bg-gray-100 disabled:opacity-50"
                        >
                          {{ t }}
                        </button>
                      } @empty {
                        <span class="text-xs text-gray-400">—</span>
                      }
                    </div>
                  </td>
                }
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class ShipmentListComponent {
  private readonly shipments$ = inject(ShipmentService);
  private readonly auth = inject(AuthService);
  private readonly analyticsStore = inject(AnalyticsStore);
  private readonly warehousesStore = inject(WarehousesStore);
  private readonly fb = inject(NonNullableFormBuilder);

  /** Status-change controls are visible only to Operator/Admin (mirrors the API's [Authorize]). */
  protected readonly canManage = computed(() => this.auth.hasRole('Operator', 'Admin'));
  protected readonly acting = signal(false);

  // Create-shipment form (Operator/Admin). Warehouses (shared store) populate the dropdowns.
  protected readonly warehouses = this.warehousesStore.items;
  protected readonly creating = signal(false);
  protected readonly createMsg = signal<{ ok: boolean; text: string } | null>(null);
  protected readonly modes = [
    { value: TransportMode.Road, label: 'Road' },
    { value: TransportMode.Rail, label: 'Rail' },
    { value: TransportMode.Sea, label: 'Sea' },
    { value: TransportMode.Air, label: 'Air' },
    { value: TransportMode.Intermodal, label: 'Intermodal' },
  ];
  protected readonly createForm = this.fb.group({
    trackingNumber: ['', [Validators.required, Validators.maxLength(64)]],
    originWarehouseId: ['', [Validators.required]],
    destinationWarehouseId: ['', [Validators.required]],
    // E.164: leading '+' then up to 15 digits — matches the API's CustomerPhone rule.
    customerPhone: ['', [Validators.required, Validators.pattern(/^\+[1-9]\d{1,14}$/)]],
    weightKg: [1, [Validators.required, Validators.min(0.1)]],
    mode: [TransportMode.Road, [Validators.required]],
  });

  protected readonly chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { display: false } },
    scales: { y: { beginAtZero: true } },
  };

  /** Breakdown chart derived from the shared analytics store (cached across views). */
  protected readonly statusChart = computed<ChartData<'bar'> | null>(() => {
    const agg = this.analyticsStore.aggregates();
    if (!agg || agg.total === 0) return null;
    return {
      labels: agg.byStatus.map((s) => s.status),
      datasets: [{ data: agg.byStatus.map((s) => s.count), backgroundColor: STATUS_COLORS }],
    };
  });

  protected readonly statuses = [
    { value: ShipmentStatus.Created, label: 'Created' },
    { value: ShipmentStatus.InTransit, label: 'In transit' },
    { value: ShipmentStatus.Delayed, label: 'Delayed' },
    { value: ShipmentStatus.Delivered, label: 'Delivered' },
    { value: ShipmentStatus.Cancelled, label: 'Cancelled' },
  ];

  protected status = ShipmentStatus.Created;
  protected readonly shipments = signal<Shipment[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  constructor() {
    this.load();
    this.analyticsStore.load();
    if (this.canManage()) {
      this.warehousesStore.load();
    }
  }

  protected createShipment(): void {
    if (this.createForm.invalid) return;
    const v = this.createForm.getRawValue();
    if (v.originWarehouseId === v.destinationWarehouseId) {
      this.createMsg.set({ ok: false, text: 'Origin and destination must differ.' });
      return;
    }
    this.creating.set(true);
    this.createMsg.set(null);
    this.shipments$.create(v).subscribe({
      next: () => {
        this.creating.set(false);
        this.createMsg.set({ ok: true, text: 'Shipment created.' });
        this.createForm.reset();
        this.load();
        this.analyticsStore.reload();
      },
      error: (err) => {
        this.creating.set(false);
        this.createMsg.set({
          ok: false,
          text: err.error?.error ?? err.error?.detail ?? 'Failed to create shipment.',
        });
      },
    });
  }


  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.shipments$.listByStatus(this.status).subscribe({
      next: (data) => {
        this.shipments.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load shipments.');
        this.loading.set(false);
      },
    });
  }

  /** Valid status transitions for a shipment, by its current status (status arrives as its name). */
  protected transitionsFor(status: string): Transition[] {
    switch (status) {
      case 'Created':
        return ['Dispatch', 'Cancel'];
      case 'InTransit':
        return ['Deliver', 'Delay'];
      case 'Delayed':
        return ['Deliver'];
      default:
        return [];
    }
  }

  protected act(shipment: Shipment, transition: Transition): void {
    this.acting.set(true);
    this.error.set(null);
    this.shipments$.updateStatus(shipment.id, transition).subscribe({
      next: () => {
        this.acting.set(false);
        this.load();
        this.analyticsStore.reload();
      },
      error: () => {
        // A 403 here means the token lacks the role — the UI gate and the API gate disagree.
        this.error.set('Status update failed (you may not have permission).');
        this.acting.set(false);
      },
    });
  }
}
