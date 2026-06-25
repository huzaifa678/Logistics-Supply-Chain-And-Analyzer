import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Role } from '../models/auth.model';

/** Mirrors the API's UserDto (GET /api/users). */
export interface ManagedUser {
  id: string;
  email: string;
  displayName: string;
  role: Role;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/api/users`;

  list(): Observable<ManagedUser[]> {
    return this.http.get<ManagedUser[]>(this.baseUrl);
  }

  updateRole(id: string, role: Role): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${encodeURIComponent(id)}/role`, { role });
  }
}
