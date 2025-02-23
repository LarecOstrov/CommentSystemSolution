import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-pagination',
  templateUrl: './pagination.component.html',
  styleUrls: ['./pagination.component.scss'],
})
export class PaginationComponent {
  @Input() currentPage!: number;
  @Input() totalPages!: number;
  @Input() hasNextPage!: boolean;
  @Input() hasPreviousPage!: boolean;
  @Input() afterCursor!: string | null;
  @Input() beforeCursor!: string | null;
  @Output() pageChange = new EventEmitter<{ page: number, afterCursor: string | null, beforeCursor: string | null }>();

  goToFirstPage() { 
    this.pageChange.emit({ page: 1, afterCursor: null, beforeCursor: null });
  }

  nextPage() { 
    if (this.hasNextPage && this.afterCursor) {
      this.pageChange.emit({ page: this.currentPage + 1, afterCursor: this.afterCursor, beforeCursor: null });
    }
  }

  previousPage() { 
    if (this.currentPage > 1 && this.beforeCursor) {
      this.pageChange.emit({ page: this.currentPage - 1, afterCursor: null, beforeCursor: this.beforeCursor });
    }
  }

  goToLastPage() { 
    this.pageChange.emit({ page: this.totalPages, afterCursor: null, beforeCursor: null });
  }
}
