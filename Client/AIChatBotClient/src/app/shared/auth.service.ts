import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';

export const API_BASE_URL = 'https://localhost:7150';
export const AUTH_TOKEN_STORAGE_KEY = 'ai-chatbot.auth-token';

export interface AuthUser {
  id: string;
  email: string;
}

export interface AuthResponse extends AuthUser {
  token: string;
  expiresAt: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly baseUrl = `${API_BASE_URL}/api/auth`;

  constructor(private readonly http: HttpClient) {}

  register(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, { email, password });
  }

  login(email: string, password: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, { email, password });
  }

  currentUser(): Observable<AuthUser> {
    return this.http.get<AuthUser>(`${this.baseUrl}/me`);
  }

  saveToken(token: string): void {
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, token);
  }

  logout(): void {
    sessionStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
  }
}
