import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PaginationComponent } from './pagination.component';
import { CommonModule } from '@angular/common';

describe('PaginationComponent', () => {
  let component: PaginationComponent;
  let fixture: ComponentFixture<PaginationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule, PaginationComponent],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(PaginationComponent);
    component = fixture.componentInstance;
    component.currentPage = 1;
    component.totalPages = 10;
    component.hasNextPage = true;
    component.hasPreviousPage = false;
    component.afterCursor = 'cursor1';
    component.beforeCursor = null;
    fixture.detectChanges();
  });

  it('should create PaginationComponent', () => {
    expect(component).toBeTruthy();
  });

  it('should emit pageChange on nextPage()', () => {
    spyOn(component.pageChange, 'emit');
    component.nextPage();
    expect(component.pageChange.emit).toHaveBeenCalledWith({ page: 2, afterCursor: 'cursor1', beforeCursor: null });
  });

  it('should emit pageChange on previousPage()', () => {
    spyOn(component.pageChange, 'emit');
    component.currentPage = 2;
    component.beforeCursor = 'cursor0';
    component.previousPage();
    expect(component.pageChange.emit).toHaveBeenCalledWith({ page: 1, afterCursor: null, beforeCursor: 'cursor0' });
  });
});
