import { TestBed } from '@angular/core/testing';
import { AppComponent } from './app.component';
import { SortingComponent } from './components/sorting/sorting.component';
import { PaginationComponent } from './components/pagination/pagination.component';
import { CommentFormComponent } from './components/comment-form/comment-form.component';
import { CommentListComponent } from './components/comment-list/comment-list.component';
import { SkeletonLoaderComponent } from './components/skeleton-loader/skeleton-loader.component';
import { CommonModule } from '@angular/common';
import { Apollo } from 'apollo-angular';
import { of } from 'rxjs';
import { WebSocketService } from './services/websocket.service';

describe('AppComponent', () => {
  let component: AppComponent;

  const mockApollo = {
    watchQuery: jasmine.createSpy('watchQuery').and.returnValue({
      valueChanges: of({
        data: {
          comments: {
            nodes: [],
            pageInfo: { hasNextPage: false, hasPreviousPage: false, endCursor: null, startCursor: null },
            totalCount: 0
          }
        }
      }),
    }),
  };

  const mockWebSocketService = {
    newComment$: of(null),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        CommonModule,
        AppComponent,
        SortingComponent,
        PaginationComponent,
        CommentFormComponent,
        CommentListComponent,
        SkeletonLoaderComponent,
      ],
      providers: [
        { provide: Apollo, useValue: mockApollo },
        { provide: WebSocketService, useValue: mockWebSocketService },
      ],
    }).compileComponents();
  });

  beforeEach(() => {
    const fixture = TestBed.createComponent(AppComponent);
    component = fixture.componentInstance;
  });

  it('should create the app', () => {
    expect(component).toBeTruthy();
  });

  it(`should have the title 'Speaking Room'`, () => {
    expect(component.title).toEqual('Speaking Room');
  });

  it('should render title', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('h1')?.textContent).toContain('Speaking Room');
  });

  it('should toggle comment form visibility', () => {
    expect(component.isCommentFormVisible).toBeFalse();
    component.toggleCommentForm();
    expect(component.isCommentFormVisible).toBeTrue();
    component.toggleCommentForm();
    expect(component.isCommentFormVisible).toBeFalse();
  });

  it('should update sorting on sort change', () => {
    component.onSortChange({ field: 'userName', order: 'ASC' });
    expect(component.sortBy).toBe('userName');
    expect(component.sortOrder).toBe('ASC');
  });

  it('should update pagination on page change', () => {
    component.onPageChange({ page: 2, afterCursor: 'cursor123', beforeCursor: null });
    expect(component.currentPage).toBe(2);
    expect(component.afterCursor).toBe('cursor123');
    expect(component.beforeCursor).toBeNull();
  });

  it('should fetch comments on initialization', () => {
    component.ngOnInit();
    expect(mockApollo.watchQuery).toHaveBeenCalled();
  });

  it('should clean up subscriptions on destroy', () => {
    spyOn(component['commentSubscription'], 'unsubscribe');
    component.ngOnDestroy();
    expect(component['commentSubscription'].unsubscribe).toHaveBeenCalled();
  });
});
