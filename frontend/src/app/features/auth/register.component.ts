import { Component, inject, signal } from "@angular/core";
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { ActivatedRoute, Router } from "@angular/router";
import { AuthService } from "../../core/services/auth.service";

@Component({
  selector: "app-register",
  imports: [ReactiveFormsModule],
  template: `
    <div class="flex min-h-dvh items-center justify-center bg-gray-50 px-4">
      <form
        [formGroup]="form"
        (ngSubmit)="submit()"
        class="w-full max-w-sm space-y-4 rounded-xl border border-gray-200 bg-white p-6 shadow-sm"
      >
        <h1 class="text-xl font-semibold tracking-tight">Sign up</h1>

        <label class="block text-sm">
          <span class="text-gray-600">Email</span>
          <input
            type="email"
            formControlName="email"
            autocomplete="username"
            class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
          />
        </label>

        <label class="block text-sm">
          <span class="text-gray-600">Password</span>
          <input
            type="password"
            formControlName="password"
            autocomplete="new-password"
            class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
          />
        </label>

        <label class="block text-sm">
          <span class="text-gray-600">Display Name</span>
          <input
            type="text"
            formControlName="displayName"
            autocomplete="name"
            class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
          />
        </label>

        @if (error()) {
          <p class="text-sm text-red-600">{{ error() }}</p>
        }

        <button
          type="submit"
          [disabled]="form.invalid || loading()"
          class="w-full rounded-md bg-blue-600 px-3 py-2 text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {{ loading() ? 'Signing up…' : 'Sign up' }}
        </button>

        <button
          type="button"
          class="px-4 py-2 text-sm text-gray-600 hover:underline"
          (click)="router.navigate(['/login'])"
        >
          Already have an account? Sign in
        </button>
      </form>
    </div>
  `,
})
export class RegisterComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  public readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
    displayName: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth.register(this.form.getRawValue()).subscribe({
      next: () => {
        this.router.navigate(['..', 'login'], { relativeTo: this.route });
      },
      error: (err) => {
        this.error.set(err.error?.message || 'An error occurred during registration.');
        this.loading.set(false);
      },
    });
  }
}
