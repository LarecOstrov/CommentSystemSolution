import { Component, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CaptchaComponent } from '../captcha/captcha.component';
import { PreviewCommentComponent } from '../preview-comment/preview-comment.component';
import { TextEditorComponent } from '../text-editor/text-editor.component';
import { SwalAlerts } from '../../utils/swal-alerts';

@Component({
  selector: 'app-comment-form',
  templateUrl: './comment-form.component.html',
  styleUrls: ['./comment-form.component.scss'],
  standalone: true,
  imports: [
    CommonModule, 
    ReactiveFormsModule,     
    CaptchaComponent, 
    PreviewCommentComponent, 
    TextEditorComponent
  ],
})
export class CommentFormComponent {
  @ViewChild('sliderRef', { static: false }) sliderRef!: ElementRef;
  @Input() parentId: string | null = null;
  @Output() commentAdded = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();

  
  selectedFiles: { file: File, name: string }[] = [];
  commentForm: FormGroup;
  isPreviewVisible = false;
  isFormSubmitted = false;
  textContent: string = '';
  charCount = 0;
  apiUrl = (window as any).env?.addCommentRest || 'http://localhost:5000/api/comments';

  constructor(private fb: FormBuilder, private http: HttpClient) { 
    this.commentForm = this.fb.group({
      userName: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9]+$/)]],
      email: ['', [Validators.required, Validators.email]],
      homePage: ['', [Validators.minLength(10), Validators.pattern(/^(http|https):\/\/[^ "]+$/)]],
      text: ['', [Validators.required, Validators.minLength(2)]],
      captchaKey: [null],
      captcha: ['', [Validators.required, Validators.minLength(6)]],
    });

    this.loadSavedFormData();
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.commentForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched) : false;
  }

  handleTextChange(newText: string) {
    this.textContent = newText;
    this.commentForm.patchValue({ text: newText });
  }

  handleCharCountChange(count: number) {
    this.charCount = count;
  }

  handleFilesChange(files: { file: File, name: string }[]) {
    this.selectedFiles = files;
  }  

  previewComment() {
    const formControls = this.commentForm.controls;  
    
    formControls['captcha'].clearValidators();
    formControls['captcha'].updateValueAndValidity();
  
    if (this.commentForm.invalid) {
      Object.keys(this.commentForm.controls).forEach(field => {
        const control = this.commentForm.get(field);
        control?.markAsTouched();
      });
      SwalAlerts.showWarning('Please fill in all required fields before previewing.');
      return;
    }
  
    this.isPreviewVisible = true;
  }
  

  editComment() {
    this.isPreviewVisible = false;
  }

  submitComment() {
    const formControls = this.commentForm.controls;

    formControls['captcha'].setValidators([Validators.required, Validators.minLength(6)]);
    formControls['captcha'].updateValueAndValidity();
    
    if (this.commentForm.invalid) {
      Object.keys(this.commentForm.controls).forEach(field => {
        const control = this.commentForm.get(field);
        control?.markAsTouched();
      });
      SwalAlerts.showInfo('Please fill in all required fields correctly.');
      return;
    }

    const formData = new FormData();
    Object.keys(this.commentForm.value).forEach((key) => {
      if (this.commentForm.value[key] !== null) {
        formData.append(key, this.commentForm.value[key]);
      }
    });

    if (this.parentId) {
      formData.append('parentId', this.parentId);
    }
    console.log(this.selectedFiles);
    this.selectedFiles.forEach((fileObj, index) => {
      formData.append(`fileAttachments`, fileObj.file, fileObj.name);
    });

    SwalAlerts.showSubmitting('Submitting...');

    this.http.post(this.apiUrl, formData).subscribe({
      next: () => {
        this.saveFormData();
        SwalAlerts.showSuccess('Your comment has been added successfully!');
        this.commentAdded.emit();
        this.commentForm.reset();

        this.isPreviewVisible = false;
        this.isFormSubmitted = true;
      },
      error: (error: HttpErrorResponse) => {
        let errorMessage = 'Failed to add comment. Please try again later.';
  
        if (error.error) {
          if (typeof error.error === 'string') {
            errorMessage = error.error; 
          } else if (error.error.errors) {
            errorMessage = Object.entries(error.error.errors)
              .map(([field, messages]) => `${field}: ${(messages as string[]).join(', ')}`)
              .join('\n');
          } else if (error.error.title) {
            errorMessage = error.error.title;
          }
        }
  
        SwalAlerts.showError(errorMessage);
      },
    });
  }

  saveFormData() {
    const formData = {
      userName: this.commentForm.value.userName,
      email: this.commentForm.value.email,
      homePage: this.commentForm.value.homePage
    };
    localStorage.setItem('commentFormData', JSON.stringify(formData));
  }

  loadSavedFormData() {
    const savedData = localStorage.getItem('commentFormData');
    if (savedData) {
      const parsedData = JSON.parse(savedData);
      this.commentForm.patchValue({
        userName: parsedData.userName || '',
        email: parsedData.email || '',
        homePage: parsedData.homePage || ''
      });
    }
  }
}
