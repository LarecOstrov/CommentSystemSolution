import { Component, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SwalAlerts } from '../../utils/swal-alerts';

@Component({
  selector: 'app-file-uploader',
  templateUrl: './file-uploader.component.html',
  styleUrls: ['./file-uploader.component.scss'],
  standalone: true,
  imports: [CommonModule],
})
export class FileUploaderComponent {
  @Input() selectedFiles: { file: File, name: string }[] = [];
  @Output() filesChange = new EventEmitter<{ file: File, name: string }[]>();
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>; 

  async onFileSelected(event: any) {
    const allowedFormats = ['image/jpeg', 'image/png', 'image/gif', 'text/plain'];
    const allowedExtensions = ['jpg', 'jpeg', 'png', 'gif', 'txt'];
    if (event.target.files.length + this.selectedFiles.length > 6) {
      SwalAlerts.showInfo('Maximum 6 files allowed.');
      event.target.value = null;
      return;
    }   

    const files: File[] = Array.from(event.target.files);
    const validFiles: { file: File, name: string }[] = [];

    for (const file of files) {
      let finalFile = file;

      if (!allowedFormats.includes(file.type)) {
        SwalAlerts.showInfo('Invalid file format: ' + file.name);
        event.target.value = null;
        return;
      }
      
      const fileExtension = file.name.split('.').pop()?.toLowerCase();
      if (!fileExtension || !allowedExtensions.includes(fileExtension)) {
        SwalAlerts.showInfo('Invalid file extension: ' + file.name);
        event.target.value = null;
        return;
      }

      if (file.size === 0) 
      {
        SwalAlerts.showInfo('Empty file: ' + file.name);
        event.target.value = null;
        return
      }

      
      if (file.type.includes('text') && file.size > 1024 * 100) {      
        SwalAlerts.showInfo('Text files must be less than 100KB.');
        event.target.value = null;
        return;
      }

      if (file.type.includes('image')) {
        finalFile = await this.resizeImage(file, 320, 240);
      }

      validFiles.push({ file: finalFile, name: this.truncateFileName(file.name, 50) });
    }

    this.selectedFiles = [...this.selectedFiles, ...validFiles];
    this.filesChange.emit(this.selectedFiles);
  }

  removeFile(index: number) {
    const updatedFiles = [...this.selectedFiles];
    updatedFiles.splice(index, 1);
    this.filesChange.emit(updatedFiles);
  }

  triggerFileSelection() {
    if (this.fileInput && this.fileInput.nativeElement) {
      this.fileInput.nativeElement.click();
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
}
