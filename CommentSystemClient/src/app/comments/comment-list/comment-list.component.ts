import { Component, OnInit } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-comment-list',
  templateUrl: './comment-list.component.html',
  styleUrls: ['./comment-list.component.scss'],
  standalone: true,
  imports: [CommonModule],
})
export class CommentListComponent implements OnInit {
  comments: any[] = [];
  currentPage = 1;
  pageSize = 2;
  totalComments = 0;
  totalPages = 0;
  hasNextPage = true;
  afterCursor: string | null = null; // cursor for pagination
  sortBy = 'createdAt';
  sortOrder = 'DESC';

  constructor(private apollo: Apollo) {}

  ngOnInit() {
    this.fetchComments();
  }

  fetchComments() {
    const GET_COMMENTS = gql`
      query getComments(
        $first: Int!,
        $after: String,
        $sort: [CommentSortInput!],
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
      .watchQuery({
        query: GET_COMMENTS,
        variables: {
          first: this.pageSize,
          after: this.afterCursor,
          sort: [{ createdAt: this.sortOrder }], 
          where: { parentId: { eq: null } },  
        },
      })
      .valueChanges.subscribe(({ data }: any) => {
        this.comments = data.comments.nodes || [];
        this.totalComments = data.comments.totalCount;
        this.totalPages = Math.ceil(this.totalComments / this.pageSize);
        this.hasNextPage = data.comments.pageInfo.hasNextPage;
        this.afterCursor = data.comments.pageInfo.endCursor || null;
      });
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

  toggleReplies(commentId: string) {
    const comment = this.comments.find(c => c.id === commentId);
    if (comment) {
      comment.showReplies = !comment.showReplies;
    }
  }
  
}
