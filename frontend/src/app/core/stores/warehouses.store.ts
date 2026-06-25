import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { Warehouse, WarehouseService } from '../services/warehouse.service';

interface WarehousesState {
  items: Warehouse[];
  loaded: boolean;
  loading: boolean;
}

const initial: WarehousesState = { items: [], loaded: false, loading: false };

/**
 * Single source of truth for warehouses. Root singleton, so every view (route estimate,
 * shipment create, warehouses page) reads the same cached list and a mutation anywhere
 * (reload()) updates them all. HTTP lives in WarehouseService so the auth interceptor applies.
 */
export const WarehousesStore = signalStore(
  { providedIn: 'root' },
  withState(initial),
  withMethods((store, service = inject(WarehouseService)) => {
    const fetch = (): void => {
      patchState(store, { loading: true });
      service.list().subscribe({
        next: (items) => patchState(store, { items, loaded: true, loading: false }),
        error: () => patchState(store, { loading: false }),
      });
    };
    return {
      /** Fetch once (cached); no-op if already loaded or in-flight. */
      load: (): void => {
        if (!store.loaded() && !store.loading()) fetch();
      },
      /** Force a refresh — call after creating a warehouse. */
      reload: (): void => fetch(),
    };
  }),
);
