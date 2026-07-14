import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule],
  template: `
    <div class="flex min-h-dvh items-center justify-center bg-gray-50 px-4">
      <form
        [formGroup]="step() === 'credentials' ? form : otpForm"
        (ngSubmit)="submit()"
        class="w-full max-w-sm space-y-4 rounded-xl border border-gray-200 bg-white p-6 shadow-sm"
      >
        <h1 class="text-xl font-semibold tracking-tight">Sign in</h1>

        @if (step() === 'credentials') {
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
              autocomplete="current-password"
              class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 focus:border-blue-500 focus:outline-none"
            />
          </label>
        } @else {
          <p class="text-sm text-gray-600">
            We sent a one-time code to your email and phone. Enter it below.
          </p>
          <label class="block text-sm">
            <span class="text-gray-600">Verification code</span>
            <input
              type="text"
              inputmode="numeric"
              autocomplete="one-time-code"
              formControlName="code"
              class="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 tracking-widest focus:border-blue-500 focus:outline-none"
            />
          </label>
        }

        @if (error()) {
          <p class="text-sm text-red-600">{{ error() }}</p>
        }

        @if (step() === 'credentials') {
          <button
            type="submit"
            [disabled]="form.invalid || loading()"
            class="w-full rounded-md bg-blue-600 px-3 py-2 text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {{ loading() ? 'Signing in…' : 'Sign in' }}
          </button>

          <button
            type="button"
            class="px-4 py-2 text-sm text-gray-600 hover:underline"
            (click)="router.navigate(['/register'])"
          >
            If not registered click here
          </button>
        } @else {
          <button
            type="submit"
            [disabled]="otpForm.invalid || loading()"
            class="w-full rounded-md bg-blue-600 px-3 py-2 text-white hover:bg-blue-700 disabled:opacity-50"
          >
            {{ loading() ? 'Verifying…' : 'Verify code' }}
          </button>

          <button
            type="button"
            class="px-4 py-2 text-sm text-gray-600 hover:underline"
            (click)="reset()"
          >
            Use a different account
          </button>
        }
      </form>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly auth = inject(AuthService);
  public readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  /** 'credentials' = email/password step; 'otp' = enter the one-time code. */
  protected readonly step = signal<'credentials' | 'otp'>('credentials');

  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected readonly otpForm = this.fb.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{4,10}$/)]],
  });

  protected submit(): void {
    return this.step() === 'credentials' ? this.submitCredentials() : this.submitOtp();
  }

  private submitCredentials(): void {
    if (this.form.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (res) => {
        this.loading.set(false);
        if (res.otpRequired) {
          this.step.set('otp');
        } else {
          this.navigateHome();
        }
      },
      error: () => {
        this.error.set('Invalid email or password.');
        this.loading.set(false);
      },
    });
  }

  private submitOtp(): void {
    if (this.otpForm.invalid) return;
    this.loading.set(true);
    this.error.set(null);

    this.auth
      .verifyOtp({ email: this.form.getRawValue().email, code: this.otpForm.getRawValue().code })
      .subscribe({
        next: () => this.navigateHome(),
        error: () => {
          this.error.set('Invalid or expired code.');
          this.loading.set(false);
        },
      });
  }

  protected reset(): void {
    this.step.set('credentials');
    this.otpForm.reset();
    this.error.set(null);
  }

  private navigateHome(): void {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '/dashboard';
    void this.router.navigateByUrl(returnUrl);
  }
}
