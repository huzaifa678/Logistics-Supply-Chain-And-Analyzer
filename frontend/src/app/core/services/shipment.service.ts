import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { RiskAssessment, Shipment, ShipmentStatus, TransportMode } from '../models/shipment.model';

export interface CreateShipmentRequest {
  trackingNumber: string;
  originWarehouseId: string;
  destinationWarehouseId: string;
  customerPhone: string;
  weightKg: number;
  mode: TransportMode;
}

@Injectable({ providedIn: 'root' })
export class ShipmentService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/shipments`;

  create(request: CreateShipmentRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.baseUrl, request);
  }

  getByTracking(trackingNumber: string): Observable<Shipment> {
    return this.http.get<Shipment>(`${this.baseUrl}/${encodeURIComponent(trackingNumber)}`);
  }

  /** The API streams a JSON array; HttpClient buffers it into a typed array for us. */
  listByStatus(status: ShipmentStatus): Observable<Shipment[]> {
    return this.http.get<Shipment[]>(`${this.baseUrl}/by-status/${status}`);
  }

  getRisk(id: string): Observable<RiskAssessment> {
    return this.http.get<RiskAssessment>(`${this.baseUrl}/${encodeURIComponent(id)}/risk`);
  }

  updateStatus(
    id: string,
    transition: 'Dispatch' | 'Delay' | 'Deliver' | 'Cancel',
    body: { estimatedArrival?: string; reason?: string } = {},
  ): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/${encodeURIComponent(id)}/status`, {
      transition,
      ...body,
    });
  }
}
