import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { Apollo, gql } from 'apollo-angular';
import { CommentItemComponent } from '../comment-item/comment-item.component';

const GET_COMMENTS = gql`
  query {
    comments {
      id
      userName
      email
      homePage
      text
      createdAt
    }
  }
`;

@Component({
  selector: 'app-comment-list',
  templateUrl: './comment-list.component.html',
  styleUrls: ['./comment-list.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentItemComponent],
})
export class CommentListComponent implements OnInit {
  comments: any[] = [];

  constructor(private apollo: Apollo) {}

  ngOnInit() {
    this.apollo
      .watchQuery({ query: GET_COMMENTS })
      .valueChanges.subscribe(({ data }: any) => {
        this.comments = data.comments;
      });
  }
}
