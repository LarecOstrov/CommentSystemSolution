import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentListComponent } from './comment-list.component';
import { Apollo } from 'apollo-angular';
import { WebSocketService } from '../../services/websocket.service';
import { of } from 'rxjs';
import { Comment } from '../../models/comment.model';
import { CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';

describe('CommentListComponent', () => {
  let component: CommentListComponent;
  let fixture: ComponentFixture<CommentListComponent>;
  let mockWebSocketService: jasmine.SpyObj<WebSocketService>;

  beforeEach(async () => {
    mockWebSocketService = jasmine.createSpyObj('WebSocketService', ['newComment$']);
    const mockComment: Comment = {
      id: 'test-comment-id',
      parentId: 'parent-comment-id',
      text: 'New reply message',
      user: {
        userName: 'TestUser',
        email: 'test@example.com',
        homePage: 'https://example.com',
        CreatedAt: new Date().toISOString()
      },
      fileAttachments: [],
      replies: [],
      createdAt: new Date().toISOString(),
      hasReplies: false
    };

    mockWebSocketService.newComment$ = of(mockComment);
       
    await TestBed.configureTestingModule({
      imports: [CommentListComponent],
      providers: [
        { provide: WebSocketService, useValue: mockWebSocketService },
        { provide: Apollo, useValue: {} }
      ],
      schemas: [CUSTOM_ELEMENTS_SCHEMA]
    }).compileComponents();

    fixture = TestBed.createComponent(CommentListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create component', () => {
    expect(component).toBeTruthy();
  });

  it('should subscribe to WebSocket messages on init', () => {
    spyOn(component, 'addReplyToCommentTree');
    component.ngOnInit();
    expect(component.addReplyToCommentTree).toHaveBeenCalled();
  });

  it('should unsubscribe from WebSocket on destroy', () => {
    spyOn(component.wsSubscription, 'unsubscribe');
    component.ngOnDestroy();
    expect(component.wsSubscription.unsubscribe).toHaveBeenCalled();
  });

  it('should toggle replies visibility', () => {
    component.openReplies.clear();
    component.toggleReplies('test-comment');
    expect(component.openReplies.has('test-comment')).toBeTrue();

    component.toggleReplies('test-comment');
    expect(component.openReplies.has('test-comment')).toBeFalse();
  });

  it('should add new reply from WebSocket', () => {
    spyOn(component, 'addReplyToCommentTree');
    component.ngOnInit();
    expect(component.addReplyToCommentTree).toHaveBeenCalled();
  });

  it('should change replies sort order', () => {
    component.sortRepliesOrder = 'ASC';
    component.changeRepliesSortOrder('parent-id');
    expect(component.sortRepliesOrder).toBe('DESC');
  });

  it('should handle reply form toggling', () => {
    component.openReplyForms.clear();
    component.toggleReplyForm('reply-id');
    expect(component.openReplyForms.has('reply-id')).toBeTrue();

    component.toggleReplyForm('reply-id');
    expect(component.openReplyForms.has('reply-id')).toBeFalse();
  });
});
