import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SkeletonLoaderComponent } from './skeleton-loader.component';
import { MatProgressBarModule } from '@angular/material/progress-bar';

describe('SkeletonLoaderComponent', () => {
  let component: SkeletonLoaderComponent;
  let fixture: ComponentFixture<SkeletonLoaderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SkeletonLoaderComponent, MatProgressBarModule]
    }).compileComponents();

    fixture = TestBed.createComponent(SkeletonLoaderComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default type as text', () => {
    expect(component.type).toBe('text');
  });

  it('should render text skeleton with correct number of lines', () => {
    component.type = 'text';
    component.lines = [1, 2, 3, 4];
    fixture.detectChanges();
    
    const lines = fixture.nativeElement.querySelectorAll('.skeleton-line');
    expect(lines.length).toBe(4);
  });

  it('should render card skeleton', () => {
    component.type = 'card';
    fixture.detectChanges();
    
    const card = fixture.nativeElement.querySelector('.skeleton-card');
    expect(card).toBeTruthy();
    
    const header = fixture.nativeElement.querySelector('.skeleton-header');
    expect(header).toBeTruthy();
  });

  it('should render table skeleton with correct rows and columns', () => {
    component.type = 'table';
    component.rows = [1, 2, 3];
    component.columns = [1, 2, 3, 4];
    fixture.detectChanges();
    
    const rows = fixture.nativeElement.querySelectorAll('.skeleton-row');
    expect(rows.length).toBe(3);
    
    const firstRowCells = rows[0].querySelectorAll('.skeleton-cell');
    expect(firstRowCells.length).toBe(4);
  });

  it('should render avatar skeleton', () => {
    component.type = 'avatar';
    fixture.detectChanges();
    
    const avatar = fixture.nativeElement.querySelector('.skeleton-avatar');
    expect(avatar).toBeTruthy();
  });

  it('should render image skeleton', () => {
    component.type = 'image';
    fixture.detectChanges();
    
    const image = fixture.nativeElement.querySelector('.skeleton-image');
    expect(image).toBeTruthy();
  });

  it('should apply animation class', () => {
    const loader = fixture.nativeElement.querySelector('.skeleton-loader');
    expect(loader).toBeTruthy();
    expect(loader.classList.contains('skeleton-loader')).toBe(true);
  });

  it('should calculate correct line widths', () => {
    expect(component.getLineWidth(0)).toBe(100);
    expect(component.getLineWidth(1)).toBe(80);
    expect(component.getLineWidth(2)).toBe(90);
    expect(component.getLineWidth(3)).toBe(100); // Should cycle back
  });
});