import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { SortingComponent } from './sorting/sorting.component';
import { PaginationComponent } from './pagination/pagination.component';
import { CommentFormComponent } from './comment-form/comment-form.component';
import { CommentListComponent } from './comment-list/comment-list.component';
import { SkeletonLoaderComponent } from './skeleton-loader/skeleton-loader.component';
import { CommonModule } from '@angular/common';

describe('AppComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        AppComponent,
        SortingComponent,
        PaginationComponent,
        CommentFormComponent,
        CommentListComponent,
        SkeletonLoaderComponent
      ],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it(`should have the title 'Speaking Room'`, () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('Speaking Room');
  });

  it('should render title', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Speaking Room');
  });
});
