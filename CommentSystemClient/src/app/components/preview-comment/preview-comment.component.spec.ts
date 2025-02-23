import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PreviewCommentComponent } from './preview-comment.component';
import { BbcodePipe } from "../../pipes/bbcode.pipe";
import { CommonModule } from '@angular/common';

describe('PreviewCommentComponent', () => {
  let component: PreviewCommentComponent;
  let fixture: ComponentFixture<PreviewCommentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule, BbcodePipe],
      declarations: [PreviewCommentComponent]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PreviewCommentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should correctly format the current date', () => {
    const now = component.getNow();
    expect(now).toMatch(/\d{2}\.\d{2}\.\d{2} \d{2}:\d{2}/);
  });

  it('should emit edit event when edit button is clicked', () => {
    spyOn(component.edit, 'emit');
    component.editComment();
    expect(component.edit.emit).toHaveBeenCalled();
  });

  it('should filter image attachments correctly', () => {
    const files = [{ file: new File([], 'image.jpg', { type: 'image/jpeg' }), name: 'image.jpg' }];
    const images = component.getPreviewImageAttachments(files);
    expect(images.length).toBe(1);
  });

  it('should filter text attachments correctly', () => {
    const files = [{ file: new File([], 'file.txt', { type: 'text/plain' }), name: 'file.txt' }];
    const textFiles = component.getPreviewTextAttachments(files);
    expect(textFiles.length).toBe(1);
  });
});
