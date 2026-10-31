import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, OnInit, signal, ViewChild } from '@angular/core';
import { ServiceChatBot } from './shared/service-chat-bot';
import { AuthService, AuthUser } from './shared/auth.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html'
})
export class App implements OnInit {
  @ViewChild('messageList') private messageList?: ElementRef<HTMLDivElement>;
  protected readonly title = signal('AIChatBotClient');
  user = signal<AuthUser | null>(null);
  authMode = signal<'login' | 'register'>('login');
  authBusy = signal(false);
  checkingSession = signal(true);
  authError = signal('');
  loginEmail = '';
  loginPassword = '';
  question: string = '';
  private questionHistory: string[] = [];
  private historyIndex = -1;
  private historyDraft = '';
  isRAG = false;
  uploadingPdf = signal(false);
  isPdfDragging = signal(false);
  uploadError = signal('');
  uploadResult = signal<{ fileName: string; chunkCount: number } | null>(null);
  answer: any = '';
  messages = signal<{ role: 'user' | 'bot'; content: string }[]>([]);
  isLoading = signal(false);
  constructor(
    private serviceChatBot: ServiceChatBot,
    private authService: AuthService
  ) { }

  ngOnInit(): void {
    this.authService.currentUser().subscribe({
      next: user => {
        this.user.set(user);
        this.checkingSession.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.checkingSession.set(false);
        if (error.status !== 401) {
          this.authError.set('Unable to connect to the sign-in service. Please try again.');
        }
      }
    });
  }

  submitAuth(): void {
    if (this.authBusy()) return;

    this.authBusy.set(true);
    this.authError.set('');
    const request = this.authMode() === 'register'
      ? this.authService.register(this.loginEmail, this.loginPassword)
      : this.authService.login(this.loginEmail, this.loginPassword);

    request.subscribe({
      next: user => {
        this.authService.saveToken(user.token);
        this.user.set(user);
        this.loginPassword = '';
        this.authBusy.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 409) {
          this.authError.set('An account with this email already exists. Try signing in instead.');
        } else if (error.status === 401) {
          this.authError.set('That email and password combination does not match.');
        } else if (error.status === 0) {
          this.authError.set('Could not reach the server. Check that the AI service is running.');
        } else {
          this.authError.set(error.error?.error ?? 'Something went wrong. Please try again.');
        }
        this.authBusy.set(false);
      }
    });
  }

  switchAuthMode(mode: 'login' | 'register'): void {
    this.authMode.set(mode);
    this.authError.set('');
  }

  logout(): void {
    this.authService.logout();
    this.user.set(null);
    this.messages.set([]);
    this.isRAG = false;
    this.uploadResult.set(null);
    this.uploadError.set('');
    this.authMode.set('login');
    this.authError.set('');
    this.question = '';
    this.questionHistory = [];
    this.historyIndex = -1;
    this.historyDraft = '';
    this.answer = '';
  }

  onPdfSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (file) {
      this.uploadPdf(file);
    }
  }

  onPdfDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isPdfDragging.set(true);
  }

  onPdfDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isPdfDragging.set(false);
  }

  onPdfDrop(event: DragEvent): void {
    event.preventDefault();
    this.isPdfDragging.set(false);
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.uploadPdf(file);
    }
  }

  private uploadPdf(file: File): void {
    if (this.uploadingPdf()) return;

    this.uploadError.set('');
    this.uploadResult.set(null);

    if (!file.name.toLowerCase().endsWith('.pdf')) {
      this.uploadError.set('Please choose a PDF file.');
      return;
    }

    if (file.size > 25 * 1024 * 1024) {
      this.uploadError.set('PDF files must be 25 MB or smaller.');
      return;
    }

    this.uploadingPdf.set(true);
    this.serviceChatBot.uploadPdf(file).subscribe({
      next: result => {
        this.uploadResult.set({ fileName: result.fileName, chunkCount: result.chunkCount });
        this.isRAG = true;
        this.uploadingPdf.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.uploadError.set(
          error.error?.error ?? (error.status === 0
            ? 'Could not reach the server. Check that the AI service is running.'
            : 'The PDF could not be uploaded. Please try again.')
        );
        this.uploadingPdf.set(false);
      }
    });
  }

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
      (response: { answer: string; isRag: boolean }) => {
        
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
