import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentFormComponent } from './comment-form.component';
import { ReactiveFormsModule } from '@angular/forms';
import { CaptchaComponent } from '../captcha/captcha.component';
import { PreviewCommentComponent } from '../preview-comment/preview-comment.component';
import { TextEditorComponent } from '../text-editor/text-editor.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { HttpClient } from '@angular/common/http';

describe('CommentFormComponent', () => {
  let component: CommentFormComponent;
  let fixture: ComponentFixture<CommentFormComponent>;
  let httpClientSpy: jasmine.SpyObj<HttpClient>;

  beforeEach(async () => {
    httpClientSpy = jasmine.createSpyObj('HttpClient', ['post']);

    await TestBed.configureTestingModule({
      imports: [CommentFormComponent, ReactiveFormsModule],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: HttpClient, useValue: httpClientSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CommentFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize the form with default values', () => {
    expect(component.commentForm).toBeDefined();
    expect(component.commentForm.get('userName')?.value).toBe('');
    expect(component.commentForm.get('email')?.value).toBe('');
    expect(component.commentForm.get('text')?.value).toBe('');
  });

  it('should emit event when a comment is successfully submitted', () => {
    spyOn(component.commentAdded, 'emit');

    httpClientSpy.post.and.returnValue(of({})); // Mock successful response
    component.submitComment();

    expect(component.commentAdded.emit).toHaveBeenCalled();
  });

  it('should show error when submission fails', () => {
    spyOn(window, 'alert'); // Mock alert
    httpClientSpy.post.and.returnValue(throwError(() => ({ error: { message: 'Invalid CAPTCHA' } })));

    component.submitComment();

    expect(window.alert).toHaveBeenCalledWith('Invalid CAPTCHA');
  });

  it('should validate required fields', () => {
    component.commentForm.patchValue({
      userName: '',
      email: '',
      text: ''
    });

    expect(component.commentForm.valid).toBeFalse();
    expect(component.isFieldInvalid('userName')).toBeTrue();
    expect(component.isFieldInvalid('email')).toBeTrue();
    expect(component.isFieldInvalid('text')).toBeTrue();
  });

  it('should toggle preview mode', () => {
    component.previewComment();
    expect(component.isPreviewVisible).toBeTrue();

    component.editComment();
    expect(component.isPreviewVisible).toBeFalse();
  });

  it('should clear the form after submission', () => {
    component.commentForm.patchValue({
      userName: 'Test User',
      email: 'test@example.com',
      text: 'This is a test comment'
    });

    httpClientSpy.post.and.returnValue(of({}));
    component.submitComment();

    expect(component.commentForm.get('userName')?.value).toBe('');
    expect(component.commentForm.get('email')?.value).toBe('');
    expect(component.commentForm.get('text')?.value).toBe('');
  });
});
