import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-sorting',
  templateUrl: './sorting.component.html',
  styleUrls: ['./sorting.component.scss'],
})
export class SortingComponent {
  @Input() sortBy!: string;
  @Input() sortOrder!: 'ASC' | 'DESC';
  @Output() sortChange = new EventEmitter<{ field: string, order: 'ASC' | 'DESC' }>();
  @Input() isCommentFormVisible!: boolean;
  @Output() toggleCommentForm = new EventEmitter<void>();

  setSort(field: string) {
    const newOrder = this.sortBy === field ? (this.sortOrder === 'ASC' ? 'DESC' : 'ASC') : 'ASC';
    this.sortChange.emit({ field, order: newOrder });
  }

  onToggleCommentForm() {
    this.toggleCommentForm.emit();
  }
}
