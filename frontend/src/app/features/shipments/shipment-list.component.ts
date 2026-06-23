import { Component, computed, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ShipmentService } from '../../core/services/shipment.service';
import { AuthService } from '../../core/services/auth.service';
import { Shipment, ShipmentStatus } from '../../core/models/shipment.model';

type Transition = 'Dispatch' | 'Delay' | 'Deliver' | 'Cancel';

@Component({
  selector: 'app-shipment-list',
  imports: [FormsModule, DatePipe],
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

  /** Status-change controls are visible only to Operator/Admin (mirrors the API's [Authorize]). */
  protected readonly canManage = computed(() => this.auth.hasRole('Operator', 'Admin'));
  protected readonly acting = signal(false);

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
      },
      error: () => {
        // A 403 here means the token lacks the role — the UI gate and the API gate disagree.
        this.error.set('Status update failed (you may not have permission).');
        this.acting.set(false);
      },
    });
  }
}
