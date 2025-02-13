import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommentFormComponent } from './comments/comment-form/comment-form.component';
import { CommentListComponent } from './comments/comment-list/comment-list.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss'],
  standalone: true,
  imports: [CommonModule, CommentFormComponent, CommentListComponent], 
})
export class AppComponent {
  title = 'Speaking Room';
  sortBy = 'createdAt';
  sortOrder: 'ASC' | 'DESC' = 'DESC';
  isCommentFormVisible = false;

  sortByCreatedAt() {  
    if (this.sortBy === 'createdAt') {
      this.toggleSortOrder();
    }  
    else{
      this.sortBy = 'createdAt';
      this.sortOrder = 'DESC';
    }
  }

  sortByUserName() {
    if (this.sortBy === 'userName') {
      this.toggleSortOrder();
    }
    else{
      this.sortBy = 'userName';
      this.sortOrder = 'ASC';
    }
  }

  sortByEmail() {
    if (this.sortBy === 'email') {
      this.toggleSortOrder();
    }
    else{
      this.sortBy = 'email';
      this.sortOrder = 'ASC';
    }    
  }

  toggleSortOrder() {
    this.sortOrder = this.sortOrder === 'ASC' ? 'DESC' : 'ASC';
  }

  toggleCommentForm() {
    this.isCommentFormVisible = !this.isCommentFormVisible;

  }

  
}
