export interface ShortestPath {
  warehouseIds: string[];
  totalDistanceKm: number;
  hops: number;
}

export interface RouteEstimate {
  warehouseIds: string[];
  totalDistanceKm: number;
  hops: number;
  estimatedHours: number;
  estimatedCost: number;
  mode: string;
}
