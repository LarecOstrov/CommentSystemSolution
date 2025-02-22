import { Component, Input, Output, EventEmitter, ViewChild, ElementRef} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { BbcodePipe } from '../../pipes/bbcode.pipe';
import Swal from 'sweetalert2';
import { areFilesValid } from '../../utils/filetype-utils';
import { validateBBCode, validateHtml } from '../../utils/tag-utils';

@Component({
  selector: 'app-comment-form',
  templateUrl: './comment-form.component.html',
  styleUrls: ['./comment-form.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, BbcodePipe],
})
export class CommentFormComponent {
  @ViewChild('sliderRef', { static: false }) sliderRef!: ElementRef;
  @Input() parentId: string | null = null;
  @Output() commentAdded = new EventEmitter<any>();
  @Output() cancel = new EventEmitter<void>();
  @Output() formClosed = new EventEmitter<void>();

  commentForm: FormGroup;
  selectedFiles: { file: File, name: string }[] = [];
  isFormVisible = true;
  isPreviewVisible = false;
  isLoadingCaptcha = false;
  isAttachmentsInputVisible = false;
  captchaImage: string | null = null;
  captchaRequestedAt: number | null = null; 
  captchaTimeout = 4 * 60 * 1000 + 50 * 1000; // captcha life time
  apiUrl = (window as any).env?.addCommentRest || 'http://localhost:5000/api/comments';
  captchaUrl = (window as any).env?.getCaptchaRest || 'http://localhost:5004/api/captcha';
  charCount = 0;

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
    this.requestCaptcha();    
  }

  requestCaptcha() {    
    this.isLoadingCaptcha = true;
    this.http.get(this.captchaUrl).subscribe({
      next: (response: any) => {
        if (response.image && response.captchaKey ) {
          this.captchaImage = response.image;
          this.commentForm.patchValue({ captchaKey: response.captchaKey  });
          this.captchaRequestedAt = Date.now(); 
        } else {
          this.showError('Failed to load CAPTCHA. Try again.');
        }
        this.isLoadingCaptcha = false;
      },
      error: (error) => {
        this.showError('Failed to load CAPTCHA. Try again later.');
        this.isLoadingCaptcha = false;
      }
    });
  }

  handleCaptchaFocus() {
    if (this.captchaRequestedAt && Date.now() - this.captchaRequestedAt >= this.captchaTimeout) {
      this.requestCaptcha();
    }
  }

  updateCharCount() {
    this.charCount = this.commentForm.get('text')?.value.length || 0;
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.commentForm.get(fieldName);
    return field ? field.invalid && (field.dirty || field.touched) : false;
  }  

  toggleAttachmentsInput() {
    this.isAttachmentsInputVisible = !this.isAttachmentsInputVisible;
  }

  insertTag(tag: string) {
    const textControl = this.commentForm.get('text');
  
    if (textControl) {
      const textarea = document.getElementById('text') as HTMLTextAreaElement;
      if (!textarea) return;
  
      const start = textarea.selectionStart;
      const end = textarea.selectionEnd;
      const value = textControl.value;
  
      if (tag === 'a') {
        const url = prompt('Enter the URL:', 'https://');
        if (!url || !/^https?:\/\/\S+/.test(url)) {
          this.showInfo('Invalid URL');
          return;
        }
  
        let newText;
        if (start !== end) {
          newText = value.substring(0, start) + `[a=${url}]` + value.substring(start, end) + `[/a]` + value.substring(end);
        } else {
          newText = value.substring(0, start) + `[a=${url}]Link[/a]` + value.substring(end);
        }
  
        textControl.setValue(newText);
      } else {
        let newText;
        if (start !== end) {
          newText = value.substring(0, start) + `[${tag}]` + value.substring(start, end) + `[/${tag}]` + value.substring(end);
        } else {
          newText = value.substring(0, start) + `[${tag}][/` + tag + `]` + value.substring(end);
        }
  
        textControl.setValue(newText);
      }
  
      textControl.markAsTouched();
  
      setTimeout(() => {
        const newCursorPos = start + `[${tag}]`.length;
        textarea.selectionStart = newCursorPos;
        textarea.selectionEnd = newCursorPos;
        textarea.focus();
      }, 0);
    }
  } 

  async onFileSelected(event: any) {
    if (event.target.files.length + this.selectedFiles.length > 6) {
      this.showError('Maximum 6 files allowed.');
      event.target.value = null;
      return;
    }

    const files: File[] = Array.from(event.target.files);

    for (const file of files) {
      if (file.type.includes('text/plain') && file.size > 100 * 1024) {
        this.showInfo(`File ${file.name} is too large. Max 100KB allowed.`);
        continue;
      }

      if (!file.type.includes('image') && !file.type.includes('text/plain')) {
        this.showInfo(`File ${file.name} is not supported. Only images (JPG, PNG, GIF) and TXT files are allowed.`);
        continue;
      }

      let finalFile = file;
      if (file.type.includes('image')) {
        finalFile = await this.resizeImage(file, 320, 240);
      }

      this.selectedFiles.push({
        file: finalFile,
        name: this.truncateFileName(file.name, 50),
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

  previewComment() {
    const formControls = this.commentForm.controls;
    const captchaValue = formControls['captcha'].value;
    formControls['captcha'].setValidators([]);
    formControls['captcha'].updateValueAndValidity();

    if (this.commentForm.invalid) {
      Object.keys(this.commentForm.controls).forEach(field => {
        const control = this.commentForm.get(field);
        control?.markAsTouched();
      });
      Swal.fire({
        icon: 'warning',
        text: 'Please fill in all required fields before previewing.',
        confirmButtonColor: '#f0ad4e',
        customClass: { popup: 'custom-swal' }
      });
      return;
    }
    this.isPreviewVisible = true;
  }

  getNow(): string {
    const now = new Date();
    const day = String(now.getDate()).padStart(2, '0');
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const year = String(now.getFullYear()).slice(-2); 
    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');
  
    return `${day}.${month}.${year} ${hours}:${minutes}`;
  }  

  getPreviewImageAttachments(files: { file: File, name: string }[]) {
    return files
      .filter(file => file.file.type.includes('image'))
      .map(file => ({
        previewUrl: URL.createObjectURL(file.file),
        name: file.name
      }));
  }
  
  getPreviewTextAttachments(files: { file: File, name: string }[]) {
    return files.filter(file => file.file.type.includes('text/plain'));
  }

  scrollImages(direction: 'left' | 'right') {
    if (this.sliderRef && this.sliderRef.nativeElement) {
      const slider = this.sliderRef.nativeElement as HTMLElement;
      const scrollAmount = 200;
      slider.scrollBy({ 
        left: direction === 'right' ? scrollAmount : -scrollAmount, 
        behavior: 'smooth' 
      });
    }
  }

  editComment() {
    this.isPreviewVisible = false;
  }

  submitComment() {
    if (this.commentForm.invalid) {
      Object.keys(this.commentForm.controls).forEach(field => {
        const control = this.commentForm.get(field);
        control?.markAsTouched();
      });
      this.showInfo('Please fill in all required fields correctly.');
      return;
    }

    const textValue: string = this.commentForm.value.text;

    const htmlValidation = validateHtml(textValue);
    const bbcodeValidation = validateBBCode(textValue);

    if (!htmlValidation.isValid || !bbcodeValidation.isValid) {
      const errors = [...htmlValidation.errors, ...bbcodeValidation.errors];
      this.showError(`Invalid input: ${errors.join('; ')}`);
      return;
    }

    if (!areFilesValid(this.selectedFiles)) {
      this.showError('Invalid file type. Only images and text files are allowed.');
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

    this.showSubmitting('Submitting...');
  
    this.http.post(this.apiUrl, formData).subscribe({
      next: () => {
        this.saveFormData();
        this.showSuccess('Your comment has been added successfully!');
        this.commentAdded.emit();
        this.commentForm.reset();
        this.selectedFiles = [];
        this.captchaImage = null;
        this.isFormVisible = false;
        this.formClosed.emit(); 
      },
      error: (error: HttpErrorResponse) => {
        if (error.error) {
          this.showError(error.error);
        } else {
          this.showError('Failed to add comment. Please try again later.');
        }
        this.requestCaptcha();
        this.commentForm.patchValue({ captcha: '' });
      },
    });
  }  

  showSubmitting(message: string) {
    Swal.fire({      
      text: message,
      allowOutsideClick: false,
      didOpen: () => {
        Swal.showLoading();
      }
    });
  }

  showError(message: string) {
    Swal.fire({
      icon: 'error',
      text: message,
      confirmButtonColor: '#d33',
      customClass: {
        popup: 'custom-swal'
      }
    });
  }
  
  showSuccess(message: string, duration: number = 1000) {
    Swal.fire({
      icon: 'success',      
      showConfirmButton: false,
      timer: duration,
      customClass: {
        popup: 'custom-swal'
      }
    });
  } 

  showInfo(message: string) {
    Swal.fire({
      icon: 'info',
      text: message,
      confirmButtonColor: '#d33',
      customClass: {
        popup: 'custom-swal'
      }
    });
  }

  cancelForm() {
    this.cancel.emit();
    this.captchaImage = null;
    this.isFormVisible = false;
    this.formClosed.emit();
  }

  removeFile(index: number) {
    this.selectedFiles.splice(index, 1);
  }
}
