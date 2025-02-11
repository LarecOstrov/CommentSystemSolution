import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';

@Component({
  selector: 'app-comment-form',
  templateUrl: './comment-form.component.html',
  styleUrls: ['./comment-form.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
})
export class CommentFormComponent {
  @Input() parentId: string | null = null;
  @Output() commentAdded = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();

  commentForm: FormGroup;
  selectedFiles: File[] = [];
  apiUrl = (window as any).env?.addCommentRest || 'http://localhost:5000/api/comments';
  captchaImage: string | null = null;
  captchaKey: string | null = null;

  constructor(private fb: FormBuilder, private http: HttpClient) { 
    this.commentForm = this.fb.group({
      userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      homePage: ['', [Validators.minLength(10), Validators.pattern(/^(http|https):\/\/[^ "]+$/)]],
      text: ['', [Validators.required]],
      captchaKey: [null],
      captcha: ['', [Validators.required, Validators.minLength(5)]],
    });
  }

  onFileSelected(event: any) {
    if (event.target.files.length > 0) {
      this.selectedFiles = Array.from(event.target.files);
    }
  }

  requestCaptcha() {
    if (this.commentForm.invalid) return;

    this.http.get('http://localhost:5000/api/captcha').subscribe({
      next: (response: any) => {
        this.captchaImage = response.image;  // Get CAPTCHA image
        this.captchaKey = response.key;
        this.commentForm.patchValue({ captchaKey: response.key });
      },
      error: (error) => {
        alert('Failed to load CAPTCHA.');
      }
    });
  }

  submitComment() {
    if (this.commentForm.invalid) {
      alert('Check the entered data!');
      return;
    }

    const formData = new FormData();
    Object.keys(this.commentForm.value).forEach((key) => {
      formData.append(key, this.commentForm.value[key]);
    });

    if (this.parentId) {
      formData.append('parentId', this.parentId); // Add parent ID for replies
    }

    this.selectedFiles.forEach((file) => {
      formData.append('fileAttachments', file);
    });

    this.http.post(this.apiUrl, formData).subscribe({
      next: (response) => {
        alert('Comment added successfully!');
        this.commentAdded.emit(response);
        this.commentForm.reset();
        this.selectedFiles = [];
        this.captchaImage = null; // Hide CAPTCHA after successful submission
      },
      error: (error: HttpErrorResponse) => {
        alert(`Error: ${error.error}`);
      },
    });
  }

  cancelForm() {
    this.cancel.emit();
    this.captchaImage = null; // ✅ Закриваємо капчу при скасуванні
  }
}
