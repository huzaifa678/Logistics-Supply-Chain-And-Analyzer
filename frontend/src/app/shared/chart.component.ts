import { ChangeDetectionStrategy, Component, PLATFORM_ID, inject, input } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

/**
 * Thin SSR-safe wrapper around ng2-charts. chart.js needs a real <canvas>, which doesn't exist
 * during server-side rendering, so the canvas is only created in the browser. The chart then
 * renders on the client after hydration.
 */
@Component({
  selector: 'app-chart',
  imports: [BaseChartDirective],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isBrowser) {
      <canvas baseChart [type]="type()" [data]="data()" [options]="options()"></canvas>
    }
  `,
})
export class ChartComponent {
  readonly type = input.required<ChartType>();
  readonly data = input.required<ChartData>();
  readonly options = input<ChartConfiguration['options']>({ responsive: true, maintainAspectRatio: false });

  protected readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
}
