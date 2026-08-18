import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class ServiceChatBot {

  constructor(private http: HttpClient) { }
baseURL:string = 'https://localhost:7150';

  askQuestion(question: string) {
    // Implementation for asking a question 
    return this.http.post(this.baseURL + '/api/ai/ask', { prompt: question }, {
      headers: { 'Content-Type': 'application/json' },
      responseType: 'json'
    });
  }

}
