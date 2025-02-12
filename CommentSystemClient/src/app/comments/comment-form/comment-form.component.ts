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
  previewFiles: any[] = [];
  isFormVisible = false;
  isLoadingCaptcha = false;
  captchaImage: string | null = null;
  apiUrl = (window as any).env?.addCommentRest || 'http://localhost:5000/api/comments';
  captchaUrl = (window as any).env?.getCaptchaRest || 'http://localhost:5004/api/captcha';
  
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

  requestCaptcha() {
    this.isLoadingCaptcha = true;
    this.http.get(this.captchaUrl).subscribe({
      next: (response: any) => {
        this.captchaImage = response.image;
        this.commentForm.patchValue({ captchaKey: response.key });
        this.isLoadingCaptcha = false;
        this.isFormVisible = true;
      },
      error: () => {
        alert('Failed to load CAPTCHA.');
        this.isLoadingCaptcha = false;
      }
    });
  }

  insertTag(tag: string) {
    const textControl = this.commentForm.get('text');
    if (textControl) {
      const selection = `[${tag}]${textControl.value}[/${tag}]`;
      textControl.setValue(selection);
    }
  }

  onFileSelected(event: any) {
    if (event.target.files.length + this.selectedFiles.length > 6) {
      alert('Maximum 6 files allowed.');
      return;
    }

    const files: File[] = Array.from(event.target.files);

    files.forEach(file => {
      if (file.size > 100 * 1024) {
        alert(`File ${file.name} is too large. Max 100KB allowed.`);
        return;
      }

      if (!file.type.includes('image') && !file.type.includes('text/plain')) {
        alert(`File ${file.name} is not supported. Only images (JPG, PNG, GIF) and TXT files are allowed.`);
        return;
      }

      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.previewFiles.push({ type: file.type, url: e.target.result });
      };
      reader.readAsDataURL(file);

      this.selectedFiles.push(file);
    });
  }

  openLightbox(imageUrl: string) {
    const lightbox = window.open('', '_blank');
    if (lightbox) {
      lightbox.document.write(`<img src="${imageUrl}" style="width:100%; max-width:800px;" />`);
    }
  }

  submitComment() {
    if (this.commentForm.invalid) return;

    const formData = new FormData();
    Object.keys(this.commentForm.value).forEach((key) => {
      formData.append(key, this.commentForm.value[key]);
    });

    if (this.parentId) {
      formData.append('parentId', this.parentId);
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
        this.captchaImage = null;
        this.isFormVisible = false;
      },
      error: (error: HttpErrorResponse) => {
        alert(`Error: ${error.error}`);
      },
    });
  }

  cancelForm() {
    this.cancel.emit();
    this.captchaImage = null;
    this.isFormVisible = false;
  }
}
