import { Component, Input, Output, EventEmitter, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FileUploaderComponent } from '../file-uploader/file-uploader.component';
import { SwalAlerts } from '../../utils/swal-alerts';

@Component({
  selector: 'app-text-editor',
  templateUrl: './text-editor.component.html',
  styleUrls: ['./text-editor.component.scss'],
  standalone: true,
  imports: [CommonModule, FileUploaderComponent]
})
export class TextEditorComponent {
  @Input() placeholder: string = 'Write your comment...';
  @Input() text: string = '';
  @Input() selectedFiles: { file: File, name: string }[] = []; 
  @Input() isTextInvalid: boolean = false;

  @Output() textChange = new EventEmitter<string>();
  @Output() charCountChange = new EventEmitter<number>();
  @Output() filesChange = new EventEmitter<{ file: File, name: string }[]>();

  @ViewChild('textareaRef') textareaRef!: ElementRef<HTMLTextAreaElement>;
  @ViewChild('fileUploader') fileUploader!: FileUploaderComponent;

  onTextChange(event: any) {
    this.text = event.target.value;
    this.textChange.emit(this.text);
    this.charCountChange.emit(this.text.length);
  }

  insertTag(tag: string) {
    if (!this.textareaRef || !this.textareaRef.nativeElement) return;

    const textarea = this.textareaRef.nativeElement;
    const start = textarea.selectionStart;
    const end = textarea.selectionEnd;
    const value = this.text;

    let newText = '';

    if (tag === 'a') {
      const url = prompt('Enter the URL:', 'https://');
      if (!url || !/^https?:\/\/\S+/.test(url)) {
        SwalAlerts.showInfo('Invalid URL');
        return;
      }

      if (start !== end) {
        newText = value.substring(0, start) + `[a=${url}]` + value.substring(start, end) + `[/a]` + value.substring(end);
      } else {
        newText = value.substring(0, start) + `[a=${url}]Link[/a]` + value.substring(end);
      }
    } else {
      if (start !== end) {
        newText = value.substring(0, start) + `[${tag}]` + value.substring(start, end) + `[/${tag}]` + value.substring(end);
      } else {
        newText = value.substring(0, start) + `[${tag}][/` + tag + `]` + value.substring(end);
      }
    }

    this.text = newText;
    this.textChange.emit(this.text);
    this.charCountChange.emit(this.text.length);

    setTimeout(() => {
      const newCursorPos = start + `[${tag}]`.length;
      textarea.selectionStart = newCursorPos;
      textarea.selectionEnd = newCursorPos;
      textarea.focus();
    }, 0);
  }

  triggerFileInput() {
    if (this.fileUploader) {
      this.fileUploader.triggerFileSelection();
    }
  }

  handleFilesChange(files: { file: File, name: string }[]) {
    this.selectedFiles = files;
    this.filesChange.emit(files);
  }
}
