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
  selectedFiles: { file: File, name: string }[] = [];
  isFormVisible = true;
  isLoadingCaptcha = false;
  isAttachmentsInputVisible = false;
  captchaImage: string | null = null;
  apiUrl = (window as any).env?.addCommentRest || 'http://localhost:5000/api/comments';
  captchaUrl = (window as any).env?.getCaptchaRest || 'http://localhost:5004/api/captcha';
  charCount = 0;

  constructor(private fb: FormBuilder, private http: HttpClient) { 
    this.commentForm = this.fb.group({
      userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      homePage: ['', [Validators.minLength(10), Validators.pattern(/^(http|https):\/\/[^ "]+$/)]],
      text: ['', [Validators.required]],
      captchaKey: [null],
      captcha: ['', [Validators.required, Validators.minLength(5)]],
    });

    this.requestCaptcha();
  }

  requestCaptcha() {    
    this.isLoadingCaptcha = true;
    this.http.get(this.captchaUrl).subscribe({
      next: (response: any) => {
        if (response.image && response.captchaKey ) {
          this.captchaImage = response.image;
          this.commentForm.patchValue({ captchaKey: response.captchaKey  }); 
        } else {
          console.error('Captcha response missing fields:', response);
        }
        this.isLoadingCaptcha = false;
      },
      error: (error) => {
        console.error('Failed to load CAPTCHA:', error);
        alert('Failed to load CAPTCHA.');
        this.isLoadingCaptcha = false;
      }
    });
  }

  updateCharCount() {
    this.charCount = this.commentForm.get('text')?.value.length || 0;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.commentForm.get(fieldName);
    return field ? field.invalid && field.touched : false;
  }

  toggleAttachmentsInput() {
    this.isAttachmentsInputVisible = !this.isAttachmentsInputVisible;
  }

  insertTag(tag: string) {
    const textControl = this.commentForm.get('text');
    if (textControl) {
      const selection = `[${tag}]${textControl.value}[/${tag}]`;
      textControl.setValue(selection);
    }
  }

  async onFileSelected(event: any) {
    if (event.target.files.length + this.selectedFiles.length > 6) {
      alert('Maximum 6 files allowed.');
      event.target.value = null;
      return;
    }

    const files: File[] = Array.from(event.target.files);

    for (const file of files) {
      if (file.type.includes('text/plain') && file.size > 100 * 1024) {
        alert(`File ${file.name} is too large. Max 100KB allowed.`);
        continue;
      }

      if (!file.type.includes('image') && !file.type.includes('text/plain')) {
        alert(`File ${file.name} is not supported. Only images (JPG, PNG, GIF) and TXT files are allowed.`);
        continue;
      }

      let finalFile = file;
      if (file.type.includes('image')) {
        finalFile = await this.resizeImage(file, 320, 240);
      }

      this.selectedFiles.push({
        file: finalFile,
        name: this.truncateFileName(file.name, 30),
      });
    }
  }

  truncateFileName(name: string, maxLength: number): string {
    if (name.length <= maxLength) return name;
    const extension = name.split('.').pop();
    return name.substring(0, maxLength) + '...' + extension;
  }

  async resizeImage(file: File, maxWidth: number, maxHeight: number): Promise<File> {
    return new Promise((resolve) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = (event) => {
        const img = new Image();
        img.src = event.target?.result as string;
        img.onload = () => {
          const canvas = document.createElement('canvas');
          const ctx = canvas.getContext('2d');

          let width = img.width;
          let height = img.height;

          if (width > maxWidth || height > maxHeight) {
            const aspectRatio = width / height;
            if (width > height) {
              width = maxWidth;
              height = Math.round(width / aspectRatio);
            } else {
              height = maxHeight;
              width = Math.round(height * aspectRatio);
            }
          }

          canvas.width = width;
          canvas.height = height;
          ctx?.drawImage(img, 0, 0, width, height);

          canvas.toBlob((blob) => {
            if (blob) {
              const resizedFile = new File([blob], file.name, { type: file.type });
              resolve(resizedFile);
            }
          }, file.type);
        };
      };
    });
  }

  submitComment() {
    if (this.commentForm.invalid) {
      Object.keys(this.commentForm.controls).forEach(field => {
        const control = this.commentForm.get(field);
        control?.markAsTouched();
      });
      return;
    }

    const formData = new FormData();
    Object.keys(this.commentForm.value).forEach((key) => {
      formData.append(key, this.commentForm.value[key]);
    });

    if (this.parentId) {
      formData.append('parentId', this.parentId);
    }

    this.selectedFiles.forEach(({ file }) => {
      formData.append('fileAttachments', file);
    });

    this.http.post(this.apiUrl, formData).subscribe({
      next: () => {
        this.commentAdded.emit();
        this.commentForm.reset();
        this.selectedFiles = [];
        this.captchaImage = null;
        this.isFormVisible = false;
      },
      error: (error: HttpErrorResponse) => {
        console.error('Failed to add comment:', error);
        alert(`Error: ${error.message}`);
        this.requestCaptcha();
        this.commentForm.patchValue({ captcha: '' });
      },
    });
  }

  cancelForm() {
    this.cancel.emit();
    this.captchaImage = null;
    this.isFormVisible = false;
  }
}
