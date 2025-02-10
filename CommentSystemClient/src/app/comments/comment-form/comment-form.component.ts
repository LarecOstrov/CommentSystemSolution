import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

@Component({
  selector: 'app-comment-form',
  templateUrl: './comment-form.component.html',
  styleUrls: ['./comment-form.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
})
export class CommentFormComponent {
  commentForm: FormGroup;
  selectedFiles: File[] = [];
  apiUrl = (window as any).env?.addCommentRest || 'http://localhost:5000/api/comments';

  constructor(private fb: FormBuilder, private http: HttpClient) {
    this.commentForm = this.fb.group({
      userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      homePage: ['', [Validators.minLength(10), Validators.pattern(/^(http|https):\/\/[^ "]+$/)]],
      text: ['', [Validators.required]],
      captchaKey: [null, Validators.required],
      captcha: ['', [Validators.required, Validators.minLength(10)]],
    });
  }

  onFileSelected(event: any) {
    if (event.target.files.length > 0) {
      this.selectedFiles = Array.from(event.target.files);
    }
  }

  submitComment() {
    if (this.commentForm.invalid) {
      alert('Ckeck the entredata!');
      return;
    }

    const formData = new FormData();
    Object.keys(this.commentForm.value).forEach((key) => {
      formData.append(key, this.commentForm.value[key]);
    });

    this.selectedFiles.forEach((file) => {
      formData.append('fileAttachments', file);
    });

    this.http.post(this.apiUrl, formData).subscribe({
      next: (response) => {
        alert('Comment added successfully!');
        this.commentForm.reset();
        this.selectedFiles = [];
      },
      error: (error: HttpErrorResponse) => {
        alert(`Error: ${error.error}`);
      },
    });
  }
}
