import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RouteEstimate, ShortestPath } from '../models/route.model';
import { TransportMode } from '../models/shipment.model';

@Injectable({ providedIn: 'root' })
export class RouteService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/routes`;

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
