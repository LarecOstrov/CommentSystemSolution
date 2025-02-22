import {  Component, OnInit} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Apollo, gql } from 'apollo-angular';
import { CommentFormComponent } from './comments/comment-form/comment-form.component';
import { CommentListComponent } from './comments/comment-list/comment-list.component';
import { Comment, FileAttachment, FileType } from './models/comment.model';
import { SortingField } from './models/sorting-field.type';
import { WebSocketService } from './services/websocket.service';
import { mapFileType } from './utils/filetype-utils';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentFormComponent, CommentListComponent],
})
export class AppComponent implements OnInit{
  title = 'Speaking Room';
  sortBy = 'createdAt';
  sortOrder: 'ASC' | 'DESC' = 'DESC';
  isCommentFormVisible = false;

  // pagination properties
  currentPage = 1;
  pageSize = 25;
  totalPages = 0;
  hasNextPage = true;
  hasPreviousPage = false;
  afterCursor: string | null = null;
  beforeCursor: string | null = null;
  comments: Comment[] = [];
  isLoading = false;
  highlightedComments: Set<string> = new Set(); 
  highlightedReplies: Set<string> = new Set();
  constructor(private apollo: Apollo, private wsService: WebSocketService) {}

  ngOnInit() {
    this.wsService.newComment$.subscribe((comment) => {
      if (comment && comment.parentId === null) {
        this.addComentToCommentTree(comment);
      }
    });
  
    this.fetchComments();
  }

  addComentToCommentTree(comment: Comment) {  
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
      hasMoreReplies: false
    };

    if (!newComment.parentId          
        && this.sortBy === 'createdAt'
        && ((this.sortOrder === 'DESC' && this.currentPage === 1) 
          || (this.sortOrder === 'ASC' && this.currentPage === this.totalPages))) {
        this.comments = this.sortOrder === 'DESC' ? [newComment, ...this.comments] : [...this.comments, newComment];
        this.sortOrder === 'DESC' ? this.comments.pop() : this.comments.shift();

        this.highlightedComments = new Set([...this.highlightedComments, comment.id]);      

      setTimeout(() => {
        this.highlightedComments.delete(newComment.id);
      }, 3000);
    }         
  }  
    
  fetchComments() {
    this.isLoading = true;
    const GET_COMMENTS = gql`
      query getComments($first: Int, $last: Int, $after: String, $before: String, $sort: [CommentSortInput!], $where: CommentFilterInput) {
        comments(first: $first, last: $last, after: $after, before: $before, order: $sort, where: $where) {
          nodes {
            id
            text
            createdAt
            user { userName email }
            fileAttachments { type url }
            hasReplies
          }
          pageInfo { hasNextPage hasPreviousPage endCursor startCursor }
          totalCount
        }
      }
    `;

    const variables: any = {
      sort: [{ createdAt: this.sortOrder }],
      where: { parentId: { eq: null } },
    };

    if (this.currentPage === 1) {
      variables.first = this.pageSize;
      variables.after = null;
      variables.before = null;
    } else if (this.currentPage === this.totalPages) {
      variables.last = this.pageSize;
      variables.before = this.beforeCursor;
      variables.after = null;
    } else if (this.beforeCursor) {
      variables.last = this.pageSize;
      variables.before = this.beforeCursor;
      variables.after = null;
    } else {
      variables.first = this.pageSize;
      variables.after = this.afterCursor;
      variables.before = null;
    }

    this.apollo.watchQuery<{ comments: { nodes: Comment[], pageInfo: { hasNextPage: boolean, hasPreviousPage: boolean, endCursor: string | null, startCursor: string | null }, totalCount: number } }>(
      {
        query: GET_COMMENTS,
        variables,
      }
    ).valueChanges.subscribe(({ data }) => {
      if (!data || !data.comments) return;
      this.comments = data.comments.nodes;
      this.isLoading = false;
      this.totalPages = Math.ceil(data.comments.totalCount / this.pageSize);
      this.hasNextPage = data.comments.pageInfo.hasNextPage;
      this.hasPreviousPage = data.comments.pageInfo.hasPreviousPage;
      this.afterCursor = data.comments.pageInfo.endCursor || null;
      this.beforeCursor = data.comments.pageInfo.startCursor || null;
    });
  }
  
  setSort(field: string) {
    this.closeCommentForm();
    if (this.sortBy === field) {
      this.toggleSortOrder();
    } else {
      this.sortBy = field;
      this.sortOrder = field === 'createdAt' ? 'DESC' : 'ASC';
    }
    this.resetPagination();
    this.fetchComments();
  }

  toggleSortOrder() {
    this.sortOrder = this.sortOrder === 'ASC' ? 'DESC' : 'ASC';
    this.resetPagination();
    this.fetchComments();
  }

  toggleCommentForm() {
    this.isCommentFormVisible = !this.isCommentFormVisible;
  }

  closeCommentForm() {
    this.isCommentFormVisible = false;
  }

  resetPagination() {
    this.currentPage = 1;
    this.afterCursor = null;
    this.beforeCursor = null;
  }

  nextPage() {
    if (this.hasNextPage) {
      this.currentPage++;
      this.beforeCursor = null;
      this.fetchComments();
    }
  }

  previousPage() {
    if (this.currentPage > 1) {
      this.currentPage--;
      this.afterCursor = null;
      this.fetchComments();
    }
  }

  goToFirstPage() {
    this.currentPage = 1;
    this.afterCursor = null;
    this.beforeCursor = null;
    this.fetchComments();
  }

  goToLastPage() {
    this.currentPage = this.totalPages;
    this.afterCursor = null;
    this.beforeCursor = null;
    this.fetchComments();
  }
}
