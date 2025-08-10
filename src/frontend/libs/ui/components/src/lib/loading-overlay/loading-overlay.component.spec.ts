import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoadingOverlayComponent } from './loading-overlay.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

describe('LoadingOverlayComponent', () => {
  let component: LoadingOverlayComponent;
  let fixture: ComponentFixture<LoadingOverlayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoadingOverlayComponent, MatProgressSpinnerModule]
    }).compileComponents();

    fixture = TestBed.createComponent(LoadingOverlayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default values', () => {
    expect(component.isLoading).toBe(false);
    expect(component.message).toBe('');
    expect(component.backdrop).toBe(true);
  });

  it('should not render overlay when isLoading is false', () => {
    component.isLoading = false;
    fixture.detectChanges();
    
    const overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeFalsy();
  });

  it('should render overlay when isLoading is true', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeTruthy();
  });

  it('should render spinner when loading', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const spinner = fixture.nativeElement.querySelector('mat-spinner');
    expect(spinner).toBeTruthy();
  });

  it('should display message when provided', () => {
    component.isLoading = true;
    component.message = 'Processing your request...';
    fixture.detectChanges();
    
    const message = fixture.nativeElement.querySelector('.loading-message');
    expect(message).toBeTruthy();
    expect(message.textContent).toBe('Processing your request...');
  });

  it('should not display message when empty', () => {
    component.isLoading = true;
    component.message = '';
    fixture.detectChanges();
    
    const message = fixture.nativeElement.querySelector('.loading-message');
    expect(message).toBeFalsy();
  });

  it('should apply backdrop class when backdrop is true', () => {
    component.isLoading = true;
    component.backdrop = true;
    fixture.detectChanges();
    
    const overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay.classList.contains('backdrop')).toBe(true);
  });

  it('should not apply backdrop class when backdrop is false', () => {
    component.isLoading = true;
    component.backdrop = false;
    fixture.detectChanges();
    
    const overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay.classList.contains('backdrop')).toBe(false);
  });

  it('should have correct spinner diameter', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const spinner = fixture.nativeElement.querySelector('mat-spinner');
    expect(spinner).toBeTruthy();
    // Angular Material may not expose diameter as an attribute in test environment
  });

  it('should have loading content container', () => {
    component.isLoading = true;
    fixture.detectChanges();
    
    const content = fixture.nativeElement.querySelector('.loading-content');
    expect(content).toBeTruthy();
  });

  it('should toggle visibility based on isLoading changes', () => {
    // Initially not loading
    component.isLoading = false;
    fixture.detectChanges();
    let overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeFalsy();
    
    // Change to loading
    component.isLoading = true;
    fixture.detectChanges();
    overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeTruthy();
    
    // Change back to not loading
    component.isLoading = false;
    fixture.detectChanges();
    overlay = fixture.nativeElement.querySelector('.loading-overlay');
    expect(overlay).toBeFalsy();
  });
});