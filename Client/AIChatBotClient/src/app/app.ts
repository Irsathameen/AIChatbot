import { Component, signal } from '@angular/core';
import { ServiceChatBot } from './shared/service-chat-bot';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('AIChatBotClient');
  question: string = '';
  answer: any = '';
  constructor(private serviceChatBot: ServiceChatBot) { }
  askQuestion() {
    // Implementation for asking a question
    this.serviceChatBot.askQuestion(this.question).subscribe(
      response => {
        console.log('Response received:', response);
        this.answer = response;
      },
      error => {
        console.error('Error details:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        this.answer = 'Error: ' + error.message;
      }
    );
  }
}
