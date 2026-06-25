import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

/** Mirrors the API's WarehouseDto (GET /api/warehouses). */
export interface Warehouse {
  id: string;
  name: string;
  latitude: number;
  longitude: number;
  capacityUnits: number;
}

export interface CreateWarehouseRequest {
  name: string;
  latitude: number;
  longitude: number;
  capacityUnits: number;
}

@Injectable({ providedIn: 'root' })
export class WarehouseService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/warehouses`;

  list(): Observable<Warehouse[]> {
    return this.http.get<Warehouse[]>(this.baseUrl);
  }

  create(request: CreateWarehouseRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, request);
  }
}
