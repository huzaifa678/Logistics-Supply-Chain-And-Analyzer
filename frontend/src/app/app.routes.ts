import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'register',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent),
  },
  {
    path: '',
    loadComponent: () => import('./features/shell/shell.component').then((m) => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'shipments',
        loadComponent: () =>
          import('./features/shipments/shipment-list.component').then(
            (m) => m.ShipmentListComponent,
          ),
      },
      {
        path: 'routes',
        loadComponent: () =>
          import('./features/routes/route-estimate.component').then(
            (m) => m.RouteEstimateComponent,
          ),
      },
      {
        path: 'warehouses',
        loadComponent: () =>
          import('./features/warehouses/warehouse-overview.component').then(
            (m) => m.WarehouseOverviewComponent,
          ),
      },
      {
        path: 'admin/users',
        canActivate: [roleGuard('Admin')],
        loadComponent: () =>
          import('./features/admin/admin-users.component').then((m) => m.AdminUsersComponent),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
