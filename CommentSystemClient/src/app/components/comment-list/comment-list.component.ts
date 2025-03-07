import { Component, Input, ViewChildren, ElementRef, QueryList, OnInit, OnDestroy  } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { CommonModule } from '@angular/common';
import { CommentFormComponent } from '../comment-form/comment-form.component'; 
import { Comment, FileAttachment, FileType, User } from '../../models/comment.model'; 
import { BbcodePipe } from '../../pipes/bbcode.pipe';
import { mapFileType } from '../../utils/filetype-utils';
import { WebSocketService } from '../../services/websocket.service';
import { Subscription } from 'rxjs';
import { SwalAlerts } from '../../utils/swal-alerts';
import { debounceTime } from 'rxjs/operators';

@Component({
  selector: 'app-comment-list',
  templateUrl: './comment-list.component.html',
  styleUrls: ['./comment-list.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentFormComponent, BbcodePipe], 
})
export class CommentListComponent implements OnInit, OnDestroy {
  @Input() comments: Comment[] = [];
  @ViewChildren('sliderRef') sliders!: QueryList<ElementRef>;
  @Input() parentId: string | null = null;
  @Input() sortBy: string = 'createdAt';
  @Input() sortOrder: 'ASC' | 'DESC' = 'DESC';
  @Input() highlightedComments: Set<string> = new Set();
  @Input() currentPage!: number;
  @Input() totalPages!: number;
  @Input() hasNextPage!: boolean;
  repliesPageSize = 10;
  sortRepliesOrder: 'DESC' | 'ASC' = 'DESC';
  isLoadingRepliesMap: Map<string, boolean> = new Map();
  openReplyForms: Set<string> = new Set();
  openReplies: Set<string> = new Set();
  replyPagination: Map<string, { afterCursor: string | null; hasMore: boolean }> = new Map();
  wsSubscription!: Subscription;
  
  constructor(private apollo: Apollo, private wsService: WebSocketService) {}

  ngOnInit() {
    this.wsSubscription = this.wsService.newComment$
    .pipe(debounceTime(500))
    .subscribe((comment) => { 
      if (comment && comment.parentId) {                        
        this.addReplyToCommentTree(comment);               
      }
    });
  }

  ngOnDestroy() {
    if (this.wsSubscription) {
      this.wsSubscription.unsubscribe();
    }
  }  

  trackByCommentId(index: number, comment: Comment) {
    return comment.id;
  }

  addReplyToCommentTree(comment: Comment) {   
    const parentComment = this.findCommentById(comment.parentId, this.comments);
    
    if (!parentComment) return;

    const updatedParentComment = { ...parentComment, replies: [...(parentComment.replies || [])] };
    
    if (!updatedParentComment.replies.some(existingComment => existingComment.id === comment.id)) {   
  
      let attachments: FileAttachment[] = [];  
      if (comment.fileAttachments) {
        if (Array.isArray(comment.fileAttachments)) {
          attachments = comment.fileAttachments as FileAttachment[];
        } else if (typeof comment.fileAttachments === 'object' && '$values' in comment.fileAttachments) {
          attachments = (comment.fileAttachments as any).$values as FileAttachment[];
        }
      }  

      const newComment: Comment = { 
        ...comment, 
        user: comment.user as User,
        fileAttachments: attachments.map((att: FileAttachment) => ({
          id: att.id,
          commentId: att.commentId,
          url: att.url,
          type: typeof att.type === 'string' 
            ? att.type as FileType 
            : mapFileType(att.type),
          createdAt: att.createdAt
        })),
        replies: [],
        hasMoreReplies: false,
      };     
      
      updatedParentComment.replies = [newComment, ...updatedParentComment.replies];      
    }

    updatedParentComment.hasReplies = true;
      this.comments = this.comments.map(comment => 
        comment.id === updatedParentComment.id ? updatedParentComment : comment
      );    
      
    if (!this.isRepliesOpen(comment.parentId)) {
      return;      
    }  
    
    this.highlightedComments = new Set([...this.highlightedComments, comment.id]);      

    setTimeout(() => {
        this.highlightedComments.delete(comment.id);              
    }, 3000);   
  }

  toggleReplies(commentId: string) {
  
    const parentComment = this.findCommentById(commentId, this.comments);
    if (!parentComment) {
      return;
    }
  
    if (this.openReplies.has(commentId)) {
      this.openReplies.delete(commentId);
    } else {
      this.openReplies.add(commentId);
  
      if (!parentComment.replies || parentComment.replies.length === 0) {
        this.fetchReplies(commentId, null, 'DESC');
      }
    }
  }
  
  fetchReplies(parentId: string, afterCursor: string | null = null, sortOrder: 'DESC' | 'ASC' = 'DESC') {
    this.isLoadingRepliesMap.set(parentId, true);
  
    const GET_REPLIES = gql`
      query getReplies($parentId: UUID!, $first: Int!, $after: String, $order: [CommentSortInput!]) {
        comments(where: { parentId: { eq: $parentId } }, first: $first, after: $after, order: $order) {
          nodes {
            id
            text
            parentId
            createdAt
            user { userName email }
            fileAttachments { type url }
            hasReplies
          }
          pageInfo { hasNextPage endCursor }
        }
      }
    `;
  
    this.apollo.watchQuery<{ comments: { nodes: Comment[], pageInfo: { hasNextPage: boolean, endCursor: string | null } } }>(
      {
        query: GET_REPLIES,
        variables: { 
          parentId, 
          first: this.repliesPageSize, 
          after: afterCursor, 
          order: [{ createdAt: sortOrder }]
        },
        fetchPolicy: 'network-only'
      }
    ).valueChanges.subscribe({
      next: ({ data }) => {
        if (!data || !data.comments) return;
  
        let parentComment = this.findCommentById(parentId, this.comments);
        if (!parentComment) return;
  
        const updatedParentComment = { ...parentComment };
        
        updatedParentComment.replies = [];        
  
        const newReplies = data.comments.nodes.filter(reply =>
          !updatedParentComment.replies?.some(existingReply => existingReply.id === reply.id)
        );
  
        updatedParentComment.replies = [...updatedParentComment.replies, ...newReplies];
        updatedParentComment.hasMoreReplies = data.comments.pageInfo.hasNextPage;
  
        this.comments = this.comments.map(comment =>
          comment.id === updatedParentComment.id ? updatedParentComment : comment
        );
  
        this.replyPagination.set(parentId, {
          afterCursor: data.comments.pageInfo.endCursor || null,
          hasMore: data.comments.pageInfo.hasNextPage,
        });        
  
        this.isLoadingRepliesMap.set(parentId, false);
      },
      error: (err) => {
        SwalAlerts.showError('Failed to fetch replies. Please try again later.');
        this.isLoadingRepliesMap.set(parentId, false);
      }
    });
  }
  
  loadMoreReplies(parentId: string) {
    const pagination = this.replyPagination.get(parentId);
    if (!pagination || !pagination.hasMore) return;
  
    const parentCommentElement = document.getElementById(`comment-${parentId}`);
    const repliesContainer = parentCommentElement?.querySelector('.replies-container');
  
    if (!repliesContainer) return;
  
    const comments = Array.from(repliesContainer.querySelectorAll('.comment-card'));
    const lastVisibleComment = comments[comments.length - 1];
    if (!lastVisibleComment) return;
  
    const lastCommentRect = lastVisibleComment.getBoundingClientRect();
    const lastCommentTop = lastCommentRect.top + window.scrollY;
  
    this.fetchReplies(parentId, pagination.afterCursor, this.sortRepliesOrder);
  
    setTimeout(() => {
      window.scrollTo({ top: lastCommentTop, behavior: 'auto' });
    }, 200);
  }   

  changeRepliesSortOrder(parentId: string) {    
    const parentComment = this.findCommentById(parentId, this.comments);
    if (parentComment) {
      parentComment.replies = [];
    }
  
    this.sortRepliesOrder = this.sortRepliesOrder === 'ASC' ? 'DESC' : 'ASC';    
  
    this.replyPagination.set(parentId, { afterCursor: null, hasMore: true });
  
    this.fetchReplies(parentId, null, this.sortRepliesOrder);
  }

  isRepliesLoading(commentId: string): boolean {
    return this.isLoadingRepliesMap.get(commentId) || false;
  }

  findCommentById(commentId: string, comments: Comment[]): Comment | null {
    if (!Array.isArray(comments)) return null;
    let stack = [...comments];
  
    while (stack.length) {
      const comment = stack.pop();
      if (!comment) continue;
  
      if (comment.id === commentId) {
        return { ...comment, replies: [...(comment.replies || [])] };
      }
  
      if (comment.replies && comment.replies.length > 0) {
        stack.push(...comment.replies);
      }
    }  
    return null;
  }
  

  toggleReplyForm(commentId: string) {
    this.openReplyForms.has(commentId) ? this.openReplyForms.delete(commentId) : this.openReplyForms.add(commentId);
  }
  
  
  updateCommentInTree(commentId: string, updatedComment: Comment, comments: Comment[]): void {
    for (let i = 0; i < comments.length; i++) {
      if (comments[i].id === commentId) {     
        comments[i] = { ...updatedComment };
        return;
      }
      if (comments[i].replies && comments[i].replies.length > 0) {
        this.updateCommentInTree(commentId, updatedComment, comments[i].replies);
      }
    }
  }  

  collapseReplies(commentId: string) {
    const commentElement = document.getElementById(`comment-${commentId}`);
    if (commentElement) {
      commentElement.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
  
    this.openReplies.delete(commentId);
  }
 
  isReplyFormOpen(commentId: string): boolean {
    return this.openReplyForms.has(commentId);
  }  

  isRepliesOpen(commentId: string): boolean {
    return this.openReplies.has(commentId);
  }  

  onReplyAdded(parentId: string) {
    if (!this.isRepliesOpen(parentId)) {
      this.toggleReplies(parentId);
    }
    this.openReplyForms.delete(parentId);
  } 

  animateScrollUp() {
    const container = document.querySelector('.comment-stream');
    if (container) {
      container.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  currentImageIndex: { [commentId: string]: number } = {};

  getImageAttachments(attachments: FileAttachment[]) {    
    return attachments.filter(att => att.type === FileType.IMAGE);
  }

  getTextAttachments(attachments: FileAttachment[]) {
    return attachments.filter(att => att.type === FileType.TEXT);
  }

  truncateFileName(url: string, maxLength: number): string {
    const fileNameWithGuid = url.split('/').pop() || ''; 
    const parts = fileNameWithGuid.split('_'); 
    const fileName = parts.length > 1 ? parts.slice(1).join('_') : fileNameWithGuid; 
  
    if (fileName.length <= maxLength) return fileName;
  
    const dotIndex = fileName.lastIndexOf('.');
    const extension = dotIndex !== -1 ? fileName.substring(dotIndex) : ''; 
    const baseName = fileName.substring(0, dotIndex !== -1 ? dotIndex : fileName.length); 
  
    return baseName.substring(0, maxLength - extension.length) + '...' + extension;
  }

  scrollImages(commentId: string, direction: 'left' | 'right') {
    const slider = this.sliders.find(el => el.nativeElement.getAttribute('data-comment-id') === commentId);
    if (slider) {
      const scrollAmount = 200;
      slider.nativeElement.scrollBy({ 
        left: direction === 'right' ? scrollAmount : -scrollAmount, 
        behavior: 'smooth' 
      });
    }
  }
}
