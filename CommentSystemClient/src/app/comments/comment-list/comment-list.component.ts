import { Component, Input, OnInit, OnChanges, SimpleChanges } from '@angular/core';
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
export class CommentListComponent implements OnInit, OnChanges {
  comments: Comment[] = [];
  currentPage = 1;
  pageSize = 25;
  totalComments = 0;
  totalPages = 0;
  hasNextPage = true;
  afterCursor: string | null = null;

  openReplyForms: Set<string> = new Set();  // Унікальні ID коментарів, у яких відкрита форма відповіді
  openReplies: Set<string> = new Set();  // Унікальні ID коментарів, у яких відкриті replies

  @Input() sortBy: string = 'createdAt';
  @Input() sortOrder: 'ASC' | 'DESC' = 'DESC';

  constructor(private apollo: Apollo) {}

  ngOnInit() {
    this.fetchComments();
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['sortBy'] || changes['sortOrder']) {
      this.resetPagination();
      this.fetchComments();
    }
  }

  resetPagination() {
    this.currentPage = 1;
    this.afterCursor = null;
  }

  fetchComments() {
    const GET_COMMENTS = gql`
      query getComments(
        $first: Int!,
        $after: String,
        $sort: [CommentSortInput!]
        $where: CommentFilterInput
      ) {
        comments(first: $first, after: $after, order: $sort, where: $where) {
          nodes {
            id
            text
            createdAt
            user {
              userName
              email
              homePage
            }
            hasReplies
          }
          pageInfo {
            hasNextPage
            endCursor
          }
          totalCount
        }
      }
    `;

    this.apollo
      .watchQuery<{ comments: { nodes: Comment[]; pageInfo: { hasNextPage: boolean; endCursor: string | null }; totalCount: number } }>(
        {
          query: GET_COMMENTS,
          variables: {
            first: this.pageSize,
            after: this.afterCursor,
            sort: [{ [this.sortBy]: this.sortOrder }],
            where: { parentId: { eq: null }}
          },
        }
      )
      .valueChanges.subscribe(({ data }) => {
        this.comments = data.comments.nodes.map(comment => ({
          ...comment,
          replies: [],
          hasMoreReplies: comment.hasReplies
        }));
        this.totalComments = data.comments.totalCount;
        this.totalPages = Math.ceil(this.totalComments / this.pageSize);
        this.hasNextPage = data.comments.pageInfo.hasNextPage;
        this.afterCursor = data.comments.pageInfo.endCursor || null;
      });
  }

  fetchReplies(parentId: string, afterCursor: string | null = null) {
    const GET_REPLIES = gql`
      query getReplies($parentId: UUID!, $first: Int!, $after: String) {
        comments(where: { parentId: { eq: $parentId } }, first: $first, after: $after) {
          nodes {
            id
            text
            createdAt
            user {
              userName
            }
            hasReplies
          }
          pageInfo {
            hasNextPage
            endCursor
          }
        }
      }
    `;

    this.apollo
      .watchQuery<{ comments: { nodes: Comment[]; pageInfo: { hasNextPage: boolean; endCursor: string | null } } }>(
        {
          query: GET_REPLIES,
          variables: { parentId, first: this.pageSize, after: afterCursor },
        }
      )
      .valueChanges.subscribe(({ data }) => {
        const parentComment = this.comments.find(comment => comment.id === parentId);
        if (parentComment) {
          const newReplies = data.comments.nodes.filter(reply =>
            !parentComment.replies.some(existingReply => existingReply.id === reply.id)
          );
          parentComment.replies = [...parentComment.replies, ...newReplies];
          parentComment.hasMoreReplies = data.comments.pageInfo.hasNextPage;
          this.openReplies.add(parentId);  // Зберігаємо ID відкритого коментаря
        }
      });
  }

  loadMoreReplies(parentId: string, parentComment: Comment) {
    this.fetchReplies(
      parentId,
      parentComment.hasMoreReplies && parentComment.replies?.length
        ? parentComment.replies[parentComment.replies.length - 1].id
        : null
    );
  }

  toggleReplyForm(commentId: string) {
    if (this.openReplyForms.has(commentId)) {
      this.openReplyForms.delete(commentId);
    } else {
      this.openReplyForms.add(commentId);
    }
  }

  isReplyFormOpen(commentId: string): boolean {
    return this.openReplyForms.has(commentId);
  }

  toggleReplies(commentId: string) {
    if (this.openReplies.has(commentId)) {
      this.openReplies.delete(commentId);
    } else {
      this.openReplies.add(commentId);
      this.fetchReplies(commentId);
    }
  }

  isRepliesOpen(commentId: string): boolean {
    return this.openReplies.has(commentId);
  }

  onReplyAdded(parentId: string) {
    this.fetchReplies(parentId);
    this.openReplyForms.delete(parentId);
  }

  nextPage() {
    if (this.hasNextPage) {
      this.currentPage++;
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
    this.fetchComments();
  }

 
  goToLastPage() {
    this.currentPage = this.totalPages;
    this.fetchComments();
  }

  animateScrollUp() {
    const container = document.querySelector('.comment-stream');
    if (container) {
      container.scrollTo({ top: 0, behavior: 'smooth' });
    }
  }  
}
