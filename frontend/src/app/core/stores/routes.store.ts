import { inject } from '@angular/core';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { RouteService, RouteSummary } from '../services/route.service';

interface RoutesState {
  items: RouteSummary[];
  loaded: boolean;
  loading: boolean;
}

const initial: RoutesState = { items: [], loaded: false, loading: false };

/** Single source of truth for routes (shared by the dashboard; refreshed on route creation). */
export const RoutesStore = signalStore(
  { providedIn: 'root' },
  withState(initial),
  withMethods((store, service = inject(RouteService)) => {
    const fetch = (): void => {
      patchState(store, { loading: true });
      service.list().subscribe({
        next: (items) => patchState(store, { items, loaded: true, loading: false }),
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
