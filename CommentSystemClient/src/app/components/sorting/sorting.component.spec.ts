import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SortingComponent } from './sorting.component';
import { CommonModule } from '@angular/common';

describe('SortingComponent', () => {
  let component: SortingComponent;
  let fixture: ComponentFixture<SortingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommonModule, SortingComponent],
    }).compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(SortingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create SortingComponent', () => {
    expect(component).toBeTruthy();
  });

  it('should emit sortChange when sorting is changed', () => {
    spyOn(component.sortChange, 'emit');
    component.setSort('userName');
    expect(component.sortChange.emit).toHaveBeenCalledWith({ field: 'userName', order: 'ASC' });
  });

  it('should emit toggleCommentForm when "Say" button is clicked', () => {
    spyOn(component.toggleCommentForm, 'emit');
    const button = fixture.nativeElement.querySelector('.say-button');
    button.click();
    expect(component.toggleCommentForm.emit).toHaveBeenCalled();
  });
});
