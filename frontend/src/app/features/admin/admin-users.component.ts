import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ManagedUser, UserService } from '../../core/services/user.service';
import { Role } from '../../core/models/auth.model';

@Component({
  selector: 'app-admin-users',
  imports: [FormsModule, DatePipe],
  template: `
    <div class="mb-6">
      <h1 class="text-2xl font-semibold tracking-tight">User management</h1>
      <p class="mt-1 text-sm text-gray-600">Assign roles to users. Admin-only.</p>
    </div>

    @if (loading()) {
      <p class="text-sm text-gray-500">Loading…</p>
    } @else if (error()) {
      <p class="text-sm text-red-600">{{ error() }}</p>
    } @else {
      <div class="overflow-x-auto rounded-xl border border-gray-200 bg-white">
        <table class="w-full text-left text-sm">
          <thead class="border-b border-gray-200 text-gray-500">
            <tr>
              <th class="px-4 py-2 font-medium">User</th>
              <th class="px-4 py-2 font-medium">Email</th>
              <th class="px-4 py-2 font-medium">Created</th>
              <th class="px-4 py-2 font-medium">Role</th>
            </tr>
          </thead>
          <tbody>
            @for (u of users(); track u.id) {
              <tr class="border-b border-gray-100 last:border-0">
                <td class="px-4 py-2 font-medium">{{ u.displayName }}</td>
                <td class="px-4 py-2 text-gray-600">{{ u.email }}</td>
                <td class="px-4 py-2 text-gray-600">{{ u.createdAt | date: 'short' }}</td>
                <td class="px-4 py-2">
                  <div class="flex items-center gap-2">
                    <select
                      [ngModel]="u.role"
                      (ngModelChange)="changeRole(u, $event)"
                      [disabled]="savingId() === u.id"
                      class="rounded-md border border-gray-300 px-2 py-1 text-sm"
                    >
                      @for (r of roles; track r) {
                        <option [ngValue]="r">{{ r }}</option>
                      }
                    </select>
                    @if (savingId() === u.id) {
                      <span class="text-xs text-gray-400">saving…</span>
                    }
                  </div>
                </td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    }
  `,
})
export class AdminUsersComponent {
  private readonly userService = inject(UserService);

  protected readonly roles: Role[] = ['Viewer', 'Operator', 'Admin'];
  protected readonly users = signal<ManagedUser[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly savingId = signal<string | null>(null);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.userService.list().subscribe({
      next: (data) => {
        this.users.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load users.');
        this.loading.set(false);
      },
    });
  }

  protected changeRole(user: ManagedUser, role: Role): void {
    if (role === user.role) return;
    const previous = user.role;
    this.savingId.set(user.id);
    this.userService.updateRole(user.id, role).subscribe({
      next: () => {
        this.users.update((list) =>
          list.map((u) => (u.id === user.id ? { ...u, role } : u)),
        );
        this.savingId.set(null);
      },
      error: () => {
        // Revert the optimistic selection on failure.
        this.users.update((list) =>
          list.map((u) => (u.id === user.id ? { ...u, role: previous } : u)),
        );
        this.error.set(`Failed to update role for ${user.email}.`);
        this.savingId.set(null);
      },
    });
  }
}
