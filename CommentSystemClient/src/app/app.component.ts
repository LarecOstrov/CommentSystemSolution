import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommentFormComponent } from './comments/comment-form/comment-form.component';
import { CommentListComponent } from './comments/comment-list/comment-list.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, CommentFormComponent, CommentListComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'Speaking Room';
}
