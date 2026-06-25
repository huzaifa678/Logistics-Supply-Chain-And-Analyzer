import { Injectable, signal } from '@angular/core';
import { TransportMode } from '../../core/models/shipment.model';
import { RouteEstimate } from '../../core/models/route.model';

export interface RouteEstimateForm {
  origin: string;
  destination: string;
  mode: TransportMode;
}

/**
 * Holds the route-estimate form + last result. Because it's a root singleton, the state outlives
 * the component, so navigating away and back restores what the user had instead of a blank form.
 */
@Injectable({ providedIn: 'root' })
export class RouteEstimateState {
  readonly form = signal<RouteEstimateForm>({
    origin: '',
    destination: '',
    mode: TransportMode.Road,
  });
  readonly result = signal<RouteEstimate | null>(null);
}
