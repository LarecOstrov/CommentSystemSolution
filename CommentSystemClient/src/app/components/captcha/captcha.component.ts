import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { SwalAlerts } from '../../utils/swal-alerts';

@Component({
  selector: 'app-captcha',
  templateUrl: './captcha.component.html',
  styleUrls: ['./captcha.component.scss'],
  standalone: true,
  imports: [CommonModule],
})
export class CaptchaComponent {
  @Input() isCaptchaInvalid: boolean = false;
  @Output() captchaKeyChange = new EventEmitter<string>();
  @Output() captchaChange = new EventEmitter<string>();

  captchaImage: string | null = null;
  isLoadingCaptcha: boolean = false;
  captchaRequestedAt: number | null = null;
  captchaTimeout = 4 * 60 * 1000 + 50 * 1000; // captcha timeout in milliseconds

  private captchaUrl = (window as any).env?.getCaptchaRest || 'http://localhost:5004/api/captcha';

  constructor(private http: HttpClient) {
    this.requestCaptcha();
  }

  requestCaptcha() {
    this.isLoadingCaptcha = true;
    this.http.get(this.captchaUrl).subscribe({
      next: (response: any) => {
        if (response.image && response.captchaKey) {
          this.captchaImage = response.image;
          this.captchaKeyChange.emit(response.captchaKey);
          this.captchaRequestedAt = Date.now();
        } else {
          SwalAlerts.showError('Failed to load CAPTCHA. Try again.');
        }
        this.isLoadingCaptcha = false;
      },
      error: () => {
        SwalAlerts.showError('Failed to load CAPTCHA. Try again later.');
        this.isLoadingCaptcha = false;
      },
    });
  }

  handleCaptchaFocus() {
    if (this.captchaRequestedAt && Date.now() - this.captchaRequestedAt >= this.captchaTimeout) {
      this.requestCaptcha();
    }
  }

  onCaptchaInput(event: Event) {
    const inputElement = event.target as HTMLInputElement;
    if (inputElement) {
      this.captchaChange.emit(inputElement.value);
    }
  }
}
