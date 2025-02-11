import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Apollo, gql } from 'apollo-angular';
import { CommentFormComponent } from '../comment-form/comment-form.component';

const GET_COMMENTS = gql`
query {
  comments(where: { parentId: { eq: null } }) {
    nodes {
      id
      text
      createdAt
      user {
        userName
        createdAt
      }
    }
  }
}

`;

@Component({
  selector: 'app-comment-list',
  templateUrl: './comment-list.component.html',
  styleUrls: ['./comment-list.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentFormComponent],
})
export class CommentListComponent {
  @Input() comments: any[] = [];

  isFormVisible = false;
  selectedParentId: string | null = null;

  constructor(private apollo: Apollo) {}

  ngOnInit() {
    if (!this.comments || this.comments.length === 0) {
      this.apollo.watchQuery({ query: GET_COMMENTS }).valueChanges.subscribe(({ data }: any) => {
        this.comments = data.comments;
      });
    }
  }

  openCommentForm(parentId: string | null) {
    this.selectedParentId = parentId;
    this.isFormVisible = true;
  }

  closeCommentForm() {
    this.isFormVisible = false;
    this.selectedParentId = null;
  }

  onCommentAdded(newComment: any) {
    if (this.selectedParentId) {
      const parentComment = this.comments.find(c => c.id === this.selectedParentId);
      parentComment.replies.push(newComment);
    } else {
      this.comments.unshift(newComment);
    }
    this.closeCommentForm();
  }
}
