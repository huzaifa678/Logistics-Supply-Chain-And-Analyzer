import { Injectable, inject } from '@angular/core';
import { Observable, forkJoin, map } from 'rxjs';
import { ShipmentService } from './shipment.service';
import { Shipment, ShipmentStatus } from '../models/shipment.model';

export interface StatusCount {
  status: string;
  value: ShipmentStatus;
  count: number;
}

export interface WarehouseVolume {
  warehouseId: string;
  outbound: number;
  inbound: number;
  total: number;
}

export interface ShipmentAggregates {
  total: number;
  byStatus: StatusCount[];
  warehouses: WarehouseVolume[];
}

const STATUS_LABELS: readonly { value: ShipmentStatus; label: string }[] = [
  { value: ShipmentStatus.Created, label: 'Created' },
  { value: ShipmentStatus.InTransit, label: 'In transit' },
  { value: ShipmentStatus.Delayed, label: 'Delayed' },
  { value: ShipmentStatus.Delivered, label: 'Delivered' },
  { value: ShipmentStatus.Cancelled, label: 'Cancelled' },
];

/**
 * Derives dashboard/warehouse aggregates from the shipments API. There is no aggregate endpoint,
 * so we fan out one `by-status` request per status and combine the results client-side.
 */
@Injectable({ providedIn: 'root' })
export class ShipmentAnalyticsService {
  private readonly shipments = inject(ShipmentService);

  load(): Observable<ShipmentAggregates> {
    return forkJoin(STATUS_LABELS.map((s) => this.shipments.listByStatus(s.value))).pipe(
      map((lists) => this.aggregate(lists)),
    );
  }

  private aggregate(lists: Shipment[][]): ShipmentAggregates {
    const byStatus: StatusCount[] = STATUS_LABELS.map((s, i) => ({
      status: s.label,
      value: s.value,
      count: lists[i].length,
    }));

    const volumes = new Map<string, WarehouseVolume>();
    const bump = (id: string, dir: 'outbound' | 'inbound'): void => {
      if (!id) return;
      const w = volumes.get(id) ?? { warehouseId: id, outbound: 0, inbound: 0, total: 0 };
      w[dir] += 1;
      w.total += 1;
      volumes.set(id, w);
    };

    const all = lists.flat();
    for (const s of all) {
      bump(s.originWarehouseId, 'outbound');
      bump(s.destinationWarehouseId, 'inbound');
    }

    const warehouses = [...volumes.values()].sort((a, b) => b.total - a.total);
    return { total: all.length, byStatus, warehouses };
  }
}
