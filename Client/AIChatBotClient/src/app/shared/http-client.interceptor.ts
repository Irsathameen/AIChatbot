import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { API_BASE_URL, AUTH_TOKEN_STORAGE_KEY } from './auth.service';

@Injectable()
export class HttpClientInterceptor implements HttpInterceptor {
  intercept(request: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const isFormData = request.body instanceof FormData;
    const token = request.url.startsWith(`${API_BASE_URL}/api/`)
      ? sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)
      : null;
    const clonedRequest = request.clone({
      headers: isFormData
        ? request.headers.delete('Content-Type').set('Accept', 'application/json')
        : request.headers.set('Content-Type', 'application/json').set('Accept', 'application/json'),
      withCredentials: false,
      setHeaders: token ? { Authorization: `Bearer ${token}` } : {}
    });

    return next.handle(clonedRequest).pipe(
      catchError((error: HttpErrorResponse) => {
        console.error('HTTP Error:', error);
        console.error('Status:', error.status);
        console.error('Message:', error.message);
        return throwError(() => error);
      })
    );
  }
}
