import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TextEditorComponent } from './text-editor.component';
import { FileUploaderComponent } from '../file-uploader/file-uploader.component';
import { CommonModule } from '@angular/common';

describe('TextEditorComponent', () => {
  let component: TextEditorComponent;
  let fixture: ComponentFixture<TextEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule],
      declarations: [TextEditorComponent, FileUploaderComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(TextEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should emit text changes', () => {
    spyOn(component.textChange, 'emit');
    component.onTextChange({ target: { value: 'test' } });
    expect(component.textChange.emit).toHaveBeenCalledWith('test');
  });

  it('should insert tag correctly', () => {
    component.text = 'Hello';
    component.insertTag('i');
    expect(component.text).toContain('[i][/i]');
  });

  it('should call file uploader when file input is triggered', () => {
    component.fileUploader = jasmine.createSpyObj('FileUploaderComponent', ['triggerFileSelection']);
    component.triggerFileInput();
    expect(component.fileUploader.triggerFileSelection).toHaveBeenCalled();
  });

  it('should update selected files on file change', () => {
    const files = [{ file: new File([], 'file.txt', { type: 'text/plain' }), name: 'file.txt' }];
    component.handleFilesChange(files);
    expect(component.selectedFiles).toEqual(files);
  });
});
