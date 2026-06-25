import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { ShipmentAggregates, ShipmentAnalyticsService } from '../services/shipment-analytics.service';

interface AnalyticsState {
  aggregates: ShipmentAggregates | null;
  loaded: boolean;
  loading: boolean;
}

const initial: AnalyticsState = { aggregates: null, loaded: false, loading: false };

/**
 * Single source of truth for shipment aggregates (status counts + warehouse volume). Shared by
 * the dashboard, shipments and warehouses pages — one cached fan-out instead of three — and
 * refreshed (reload()) when a shipment is created or its status changes.
 */
export const AnalyticsStore = signalStore(
  { providedIn: 'root' },
  withState(initial),
  withMethods((store, service = inject(ShipmentAnalyticsService)) => {
    const fetch = (): void => {
      patchState(store, { loading: true });
      service.load().subscribe({
        next: (aggregates) => patchState(store, { aggregates, loaded: true, loading: false }),
        error: () => patchState(store, { loading: false }),
      });
    };
    return {
      load: (): void => {
        if (!store.loaded() && !store.loading()) fetch();
      },
      reload: (): void => fetch(),
    };
  }),
);
