import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouteService } from '../../core/services/route.service';
import { WarehousesStore } from '../../core/stores/warehouses.store';
import { TransportMode } from '../../core/models/shipment.model';
import { RouteEstimateState } from './route-estimate.state';

@Component({
  selector: 'app-route-estimate',
  imports: [ReactiveFormsModule, DecimalPipe],
  template: `
    <h1 class="mb-6 text-2xl font-semibold tracking-tight">Route estimate</h1>

    <form
      [formGroup]="form"
      (ngSubmit)="estimate()"
      class="grid max-w-xl gap-4 rounded-xl border border-gray-200 bg-white p-5 shadow-sm sm:grid-cols-2"
    >
      <label class="block text-sm">
        <span class="text-gray-600">Origin warehouse</span>
        <select
          formControlName="origin"
          class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
        >
          <option value="" disabled>Select origin…</option>
          @for (w of warehouses(); track w.id) {
            <option [value]="w.id">{{ w.name || w.id }}</option>
          }
        </select>
      </label>
      <label class="block text-sm">
        <span class="text-gray-600">Destination warehouse</span>
        <select
          formControlName="destination"
          class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2"
        >
          <option value="" disabled>Select destination…</option>
          @for (w of warehouses(); track w.id) {
            <option [value]="w.id">{{ w.name || w.id }}</option>
          }
        </select>
      </label>
      <label class="block text-sm">
        <span class="text-gray-600">Mode</span>
        <select formControlName="mode" class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2">
          @for (m of modes; track m.value) {
            <option [ngValue]="m.value">{{ m.label }}</option>
          }
        </select>
      </label>
      <div class="flex items-end">
        <button
          type="submit"
          [disabled]="form.invalid || loading()"
          class="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {{ loading() ? 'Estimating…' : 'Estimate' }}
        </button>
      </div>
    </form>

    @if (warehouses().length < 2) {
      <p class="mt-3 text-sm text-gray-500">
        Add at least two warehouses (and routes between them) to estimate.
      </p>
    }

    @if (error()) {
      <p class="mt-4 text-sm text-red-600">{{ error() }}</p>
    }

    @if (result(); as r) {
      <dl class="mt-6 grid max-w-xl gap-4 sm:grid-cols-3">
        <div class="rounded-xl border border-gray-200 bg-white p-4">
          <dt class="text-xs text-gray-500">Distance</dt>
          <dd class="text-lg font-semibold">{{ r.totalDistanceKm | number: '1.0-1' }} km</dd>
        </div>
        <div class="rounded-xl border border-gray-200 bg-white p-4">
          <dt class="text-xs text-gray-500">Duration</dt>
          <dd class="text-lg font-semibold">{{ r.estimatedHours | number: '1.0-2' }} h</dd>
        </div>
        <div class="rounded-xl border border-gray-200 bg-white p-4">
          <dt class="text-xs text-gray-500">Cost</dt>
          <dd class="text-lg font-semibold">{{ r.estimatedCost | number: '1.2-2' }}</dd>
        </div>
      </dl>
    }
  `,
})
export class RouteEstimateComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly routes = inject(RouteService);
  private readonly warehousesStore = inject(WarehousesStore);
  private readonly state = inject(RouteEstimateState);

  protected readonly modes = [
    { value: TransportMode.Road, label: 'Road' },
    { value: TransportMode.Rail, label: 'Rail' },
    { value: TransportMode.Sea, label: 'Sea' },
    { value: TransportMode.Air, label: 'Air' },
    { value: TransportMode.Intermodal, label: 'Intermodal' },
  ];

  protected readonly warehouses = this.warehousesStore.items;
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  // Result is held in a root service so it survives navigation away/back.
  protected readonly result = this.state.result;

  // Initialise from persisted state so revisiting the page keeps the last selection.
  protected readonly form = this.fb.group({
    origin: [this.state.form().origin, [Validators.required]],
    destination: [this.state.form().destination, [Validators.required]],
    mode: [this.state.form().mode, [Validators.required]],
  });

  constructor() {
    this.warehousesStore.load();

    // Persist every change so the form isn't lost on navigation.
    this.form.valueChanges.pipe(takeUntilDestroyed()).subscribe((v) =>
      this.state.form.set({
        origin: v.origin ?? '',
        destination: v.destination ?? '',
        mode: v.mode ?? TransportMode.Road,
      }),
    );
  }

  protected estimate(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);
    this.state.result.set(null);

    const { origin, destination, mode } = this.form.getRawValue();
    this.routes.estimate(origin, destination, mode).subscribe({
      next: (r) => {
        this.state.result.set(r);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('No route found between those warehouses.');
        this.loading.set(false);
      },
    });
  }
}
