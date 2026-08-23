import { Component, ElementRef, signal, ViewChild } from '@angular/core';
import { ServiceChatBot } from './shared/service-chat-bot';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  @ViewChild('messageList') private messageList?: ElementRef<HTMLDivElement>;
  protected readonly title = signal('AIChatBotClient');
  question: string = '';
  isRAG = false;
  answer: any = '';
  messages = signal<{ role: 'user' | 'bot'; content: string }[]>([]);
  isLoading = signal(false);
  constructor(private serviceChatBot: ServiceChatBot) { }

  formatMarkdown(content: string): string {
    const escapedContent = content
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');

    return escapedContent.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
  }

  private scrollMessagesToBottom(): void {
    requestAnimationFrame(() => {
      const messageList = this.messageList?.nativeElement;
      if (messageList) {
        messageList.scrollTop = messageList.scrollHeight;
      }
    });
  }

  askQuestion() {
    const prompt = this.question.trim();
    if (!prompt || this.isLoading()) return;

    this.messages.update(messages => [...messages, { role: 'user', content: prompt }]);
    this.scrollMessagesToBottom();
    this.question = '';
    this.isLoading.set(true);

    this.serviceChatBot.askQuestion(prompt, this.isRAG).subscribe(
      (response: { answer: string,isRAG: boolean }) => {  
        
        debugger;
        this.answer = response.answer;
        this.messages.update(messages => [...messages, { role: 'bot', content: response.answer }]);
        this.scrollMessagesToBottom();
        this.isLoading.set(false);
      },
      (error: { status?: number; message?: string }) => {
        console.error('Error details:', error);
        console.error('Error status:', error.status);
        console.error('Error message:', error.message);
        this.answer = 'Error: ' + error.message;
        this.messages.update(messages => [...messages, { role: 'bot', content: this.answer }]);
        this.scrollMessagesToBottom();
        this.isLoading.set(false);
      }
    );
  }
}
