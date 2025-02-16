import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Apollo, gql } from 'apollo-angular';
import { CommentFormComponent } from './comments/comment-form/comment-form.component';
import { CommentListComponent } from './comments/comment-list/comment-list.component';
import { Comment } from './models/comment.model';
import { SortingField } from './models/sorting-field.type';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentFormComponent, CommentListComponent],
})
export class AppComponent implements OnInit {
  title = 'Speaking Room';
  sortBy = 'createdAt';
  sortOrder: 'ASC' | 'DESC' = 'DESC';
  isCommentFormVisible = false;

  // Параметри пагінації
  currentPage = 1;
  pageSize = 5;
  totalPages = 0;
  hasNextPage = true;
  afterCursor: string | null = null;
  beforeCursor: string | null = null;
  comments: Comment[] = [];

  constructor(private apollo: Apollo) {}

  ngOnInit() {
    this.fetchComments();
  }

  fetchComments() {
    console.log(this.sortBy, this.sortOrder, this.currentPage, this.pageSize, this.afterCursor, this.beforeCursor);
  
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
  
    let sortingFields: SortingField[] = [];
    if (this.sortBy === 'createdAt') {
      sortingFields = [{ createdAt: this.sortOrder }];
    } else if (this.sortBy === 'userName') {
      sortingFields = [{ user: { userName: this.sortOrder } }, { createdAt: 'DESC' }];
    } else if (this.sortBy === 'email') {
      sortingFields = [{ user: { email: this.sortOrder } }, { createdAt: 'DESC' }];
    }
  
    const variables: any = {
      sort: sortingFields,
      where: { parentId: { eq: null } }
    };
  
    if (this.currentPage === 1) {
      variables.first = this.pageSize;
      variables.after = null;
      variables.before = null;
    } else if (this.currentPage === this.totalPages) {
      variables.last = this.pageSize;
      variables.after = null;
      variables.before = this.beforeCursor;
    } else if (this.currentPage > 1) {
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
  
      this.comments = data.comments.nodes.map(comment => ({
        ...comment,
        replies: [],
        hasMoreReplies: comment.hasReplies
      }));
  
      // Виправлений розрахунок `totalPages`
      this.totalPages = Math.ceil(data.comments.totalCount / this.pageSize);
      this.hasNextPage = data.comments.pageInfo.hasNextPage;
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
      this.beforeCursor = this.afterCursor;
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
