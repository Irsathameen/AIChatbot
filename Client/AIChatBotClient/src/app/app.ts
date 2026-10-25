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
  private questionHistory: string[] = [];
  private historyIndex = -1;
  private historyDraft = '';
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

  navigateQuestionHistory(event: KeyboardEvent): void {
    if ((event.key !== 'ArrowUp' && event.key !== 'ArrowDown') || this.questionHistory.length === 0) {
      return;
    }

    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (this.historyIndex === -1) {
        this.historyDraft = this.question;
        this.historyIndex = this.questionHistory.length;
      }
      this.historyIndex = Math.max(0, this.historyIndex - 1);
      this.question = this.questionHistory[this.historyIndex];
      return;
    }

    if (this.historyIndex === -1) {
      return;
    }

    event.preventDefault();
    this.historyIndex++;
    if (this.historyIndex >= this.questionHistory.length) {
      this.historyIndex = -1;
      this.question = this.historyDraft;
      this.historyDraft = '';
      return;
    }

    this.question = this.questionHistory[this.historyIndex];
  }

  askQuestion() {
    const prompt = this.question.trim();
    if (!prompt || this.isLoading()) return;

    this.questionHistory.push(prompt);
    this.historyIndex = -1;
    this.historyDraft = '';
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
