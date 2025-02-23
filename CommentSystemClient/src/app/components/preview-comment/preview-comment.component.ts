import { CommonModule } from '@angular/common';
import { Component, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { BbcodePipe } from "../../pipes/bbcode.pipe";

@Component({
  selector: 'app-preview-comment',
  templateUrl: './preview-comment.component.html',
  styleUrls: ['./preview-comment.component.scss'],
  standalone: true,
  imports: [CommonModule, BbcodePipe]
})
export class PreviewCommentComponent {
  @Input() commentText: string = '';
  @Input() username: string = '';
  @Input() email: string = '';
  @Input() attachments: { file: File, name: string }[] = [];
  @Output() edit = new EventEmitter<void>();

  @ViewChild('sliderRef', { static: false }) sliderRef!: ElementRef;

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

  truncateFileName(name: string, maxLength: number): string {
    if (name.length <= maxLength) return name;
    const extension = name.split('.').pop();
    return name.substring(0, maxLength) + '...' + extension;
  }

  editComment() {
    this.edit.emit();
  }
}
