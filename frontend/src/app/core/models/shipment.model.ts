export enum TransportMode {
  Road = 0,
  Rail = 1,
  Sea = 2,
  Air = 3,
  Intermodal = 4,
}

export enum ShipmentStatus {
  Created = 0,
  InTransit = 1,
  Delayed = 2,
  Delivered = 3,
  Cancelled = 4,
}

/** Mirrors the API's ShipmentDto. The API serializes the enums as their string names. */
export interface Shipment {
  id: string;
  trackingNumber: string;
  originWarehouseId: string;
  destinationWarehouseId: string;
  weightKg: number;
  mode: string;
  status: string;
  createdAt: string;
  estimatedArrival: string | null;
  deliveredAt: string | null;
}

export interface RiskFactor {
  name: string;
  points: number;
  reason: string;
}

export interface RiskAssessment {
  shipmentId: string;
  score: number;
  band: string;
  factors: RiskFactor[];
}
