import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CaptchaComponent } from './captcha.component';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { CommonModule } from '@angular/common';

describe('CaptchaComponent', () => {
  let component: CaptchaComponent;
  let fixture: ComponentFixture<CaptchaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(CaptchaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should emit captcha input change', () => {
    spyOn(component.captchaChange, 'emit');
    const inputEvent = { target: { value: '123456' } } as unknown as Event;
    component.onCaptchaInput(inputEvent);
    expect(component.captchaChange.emit).toHaveBeenCalledWith('123456');
  });

  it('should call requestCaptcha when focus is triggered after timeout', () => {
    spyOn(component, 'requestCaptcha');
    component.captchaRequestedAt = Date.now() - component.captchaTimeout;
    component.handleCaptchaFocus();
    expect(component.requestCaptcha).toHaveBeenCalled();
  });
});
