import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { API_BASE_URL } from './auth.service';

export interface PdfUploadResponse {
  message: string;
  fileName: string;
  chunkCount: number;
}

@Injectable({
  providedIn: 'root',
})
export class ServiceChatBot {

  constructor(private http: HttpClient) { }
private readonly baseURL = API_BASE_URL;

askQuestion(question: string, isRAG: boolean): Observable<{ answer: string; isRag: boolean }> {
  return this.http.post<{ answer: string; isRag: boolean }>(this.baseURL + '/api/ai/ask', {
      prompt: question,
      isRAG
    });
  }

uploadPdf(file: File): Observable<PdfUploadResponse> {
  const formData = new FormData();
  formData.append('file', file, file.name);
  return this.http.post<PdfUploadResponse>(`${this.baseURL}/api/ai/upload-pdf`, formData);
}

}
