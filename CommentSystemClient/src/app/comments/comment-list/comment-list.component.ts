import { Component, Input, ViewChildren, ElementRef, QueryList } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { CommonModule } from '@angular/common';
import { CommentFormComponent } from '../comment-form/comment-form.component'; 
import { Comment } from '../../models/comment.model'; 

@Component({
  selector: 'app-comment-list',
  templateUrl: './comment-list.component.html',
  styleUrls: ['./comment-list.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentFormComponent], 
})
export class CommentListComponent {
  @Input() comments: Comment[] = [];
  @ViewChildren('sliderRef') sliders!: QueryList<ElementRef>;
  @Input() parentId: string | null = null;
  @Input() sortBy: string = 'createdAt';
  @Input() sortOrder: 'ASC' | 'DESC' = 'DESC';
  
  @Input() currentPage!: number;
  @Input() totalPages!: number;
  @Input() hasNextPage!: boolean;
 
  openReplyForms: Set<string> = new Set();
  openReplies: Set<string> = new Set();
  replyPagination: Map<string, { afterCursor: string | null; hasMore: boolean }> = new Map();

  constructor(private apollo: Apollo) {}
  
  fetchReplies(parentId: string, afterCursor: string | null = null) {
    const GET_REPLIES = gql`
      query getReplies($parentId: UUID!, $first: Int!, $after: String) {
        comments(where: { parentId: { eq: $parentId } }, first: $first, after: $after) {
          nodes {
            id
            text
            parentId
            createdAt
            user { userName email}
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
        variables: { parentId, first: 25, after: afterCursor },
      }
    ).valueChanges.subscribe(({ data }) => {
      if (!data || !data.comments) return;

      const parentComment = this.findCommentById(parentId, this.comments);
      if (parentComment) {
        const newReplies = data.comments.nodes.filter(reply =>
          !(parentComment.replies.some(existingReply => existingReply.id === reply.id))
        );

        parentComment.replies = [...parentComment.replies, ...newReplies];
        parentComment.hasMoreReplies = data.comments.pageInfo.hasNextPage;

        this.replyPagination.set(parentId, {
          afterCursor: data.comments.pageInfo.endCursor || null,
          hasMore: data.comments.pageInfo.hasNextPage,
        });

        this.comments = [...this.comments];
      }
    });
  }

  findCommentById(commentId: string, comments: Comment[]): Comment | null {
    for (let comment of comments) {
      if (comment.id === commentId) {
        return comment;
      }
      if (comment.replies && comment.replies.length > 0) {
        const foundComment = this.findCommentById(commentId, comment.replies);
        if (foundComment) {
          return foundComment;
        }
      }
    }
    return null;
  }  

  toggleReplyForm(commentId: string) {
    this.openReplyForms.has(commentId) ? this.openReplyForms.delete(commentId) : this.openReplyForms.add(commentId);
  }

  toggleReplies(commentId: string) {
    if (this.openReplies.has(commentId)) {
      // Закриваємо та не видаляємо replies, щоб зберігати завантажені відповіді
      this.openReplies.delete(commentId);
    } else {
      this.openReplies.add(commentId);
  
      // Шукаємо коментар у дереві
      const parentComment = this.findCommentById(commentId, this.comments);
      
      if (parentComment) {
        // 🔥 Створюємо новий об'єкт для уникнення помилки "Cannot add property"
        const updatedComment = { ...parentComment };
  
        // Якщо replies не існує, ініціалізуємо його
        if (!updatedComment.replies) {
          updatedComment.replies = [];
        }
  
        // Оновлюємо дерево коментарів (іммутабельний підхід)
        this.updateCommentInTree(commentId, updatedComment, this.comments);
        
        // Виконуємо запит
        this.fetchReplies(commentId);
      }
    }
  }
  
  updateCommentInTree(commentId: string, updatedComment: Comment, comments: Comment[]): void {
  for (let i = 0; i < comments.length; i++) {
    if (comments[i].id === commentId) {
      // 🔥 Іммутабельно оновлюємо коментар
      comments[i] = { ...updatedComment };
      return;
    }
    if (comments[i].replies && comments[i].replies.length > 0) {
      this.updateCommentInTree(commentId, updatedComment, comments[i].replies);
    }
  }
}

  loadMoreReplies(parentId: string) {
    const pagination = this.replyPagination.get(parentId);
    if (pagination && pagination.hasMore) {
      this.fetchReplies(parentId, pagination.afterCursor);
    }
  }

  collapseReplies(commentId: string) {
    this.openReplies.delete(commentId);
  }
 
  isReplyFormOpen(commentId: string): boolean {
    return this.openReplyForms.has(commentId);
  }  

  isRepliesOpen(commentId: string): boolean {
    return this.openReplies.has(commentId);
  }  

  onReplyAdded(parentId: string) {
    this.fetchReplies(parentId);
    this.openReplyForms.delete(parentId);
  } 

  animateScrollUp() {
    const container = document.querySelector('.comment-stream');
    if (container) {
      container.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }

  currentImageIndex: { [commentId: string]: number } = {};

  getImageAttachments(attachments: { url: string, type: string }[]) {
    return attachments.filter(att => att.type.toLowerCase() ==='image');
  }

  getTextAttachments(attachments: { url: string, type: string }[]) {
    return attachments.filter(att => att.type.toLowerCase() === 'text');
  }

  truncateFileName(name: string, maxLength: number): string {
    if (name.length <= maxLength) return name;
    const extension = name.split('.').pop();
    return name.substring(0, maxLength) + '...' + extension;
  }

  scrollImages(commentId: string, direction: 'left' | 'right') {
    const slider = this.sliders.find(el => el.nativeElement.getAttribute('data-comment-id') === commentId);
    if (slider) {
      const scrollAmount = 200; // Крок прокрутки
      slider.nativeElement.scrollBy({ 
        left: direction === 'right' ? scrollAmount : -scrollAmount, 
        behavior: 'smooth' 
      });
    }
  }
}
