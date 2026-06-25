import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RouteEstimate, ShortestPath } from '../models/route.model';
import { TransportMode } from '../models/shipment.model';

/** Mirrors the API's RouteSummaryDto (GET /api/routes). */
export interface RouteSummary {
  id: string;
  originWarehouseId: string;
  originName: string;
  destinationWarehouseId: string;
  destinationName: string;
  distanceKm: number;
  cost: number;
  mode: string;
}

@Injectable({ providedIn: 'root' })
export class RouteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/routes`;

  list(): Observable<RouteSummary[]> {
    return this.http.get<RouteSummary[]>(this.baseUrl);
  }

  create(request: {
    originWarehouseId: string;
    destinationWarehouseId: string;
    distanceKm: number;
    cost: number;
    mode: TransportMode;
  }): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, request);
  }

  shortestPath(origin: string, destination: string): Observable<ShortestPath> {
    const params = new HttpParams().set('origin', origin).set('destination', destination);
    return this.http.get<ShortestPath>(`${this.baseUrl}/shortest-path`, { params });
  }

  estimate(origin: string, destination: string, mode: TransportMode): Observable<RouteEstimate> {
    const params = new HttpParams()
      .set('origin', origin)
      .set('destination', destination)
      .set('mode', mode);
    return this.http.get<RouteEstimate>(`${this.baseUrl}/estimate`, { params });
  }
}
