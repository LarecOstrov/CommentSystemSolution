import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FileUploaderComponent } from './file-uploader.component';
import { CommonModule } from '@angular/common';

describe('FileUploaderComponent', () => {
  let component: FileUploaderComponent;
  let fixture: ComponentFixture<FileUploaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule],
      declarations: [FileUploaderComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(FileUploaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should emit files when selected', () => {
    spyOn(component.filesChange, 'emit');
    const file = new File([''], 'file.txt', { type: 'text/plain' });
    const event = { target: { files: [file] } };
    component.onFileSelected(event);
    expect(component.filesChange.emit).toHaveBeenCalled();
  });

  it('should remove file and emit updated files', () => {
    spyOn(component.filesChange, 'emit');
    component.selectedFiles = [{ file: new File([], 'file.txt', { type: 'text/plain' }), name: 'file.txt' }];
    component.removeFile(0);
    expect(component.filesChange.emit).toHaveBeenCalledWith([]);
  });
});
