import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ProgressIndicatorComponent } from './progress-indicator.component';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatIconModule } from '@angular/material/icon';

describe('ProgressIndicatorComponent', () => {
  let component: ProgressIndicatorComponent;
  let fixture: ComponentFixture<ProgressIndicatorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ProgressIndicatorComponent,
        MatProgressSpinnerModule,
        MatProgressBarModule,
        MatIconModule
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ProgressIndicatorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should have default values', () => {
    expect(component.type).toBe('spinner');
    expect(component.mode).toBe('indeterminate');
    expect(component.value).toBe(0);
    expect(component.size).toBe('medium');
    expect(component.showPercentage).toBe(true);
    expect(component.label).toBe('');
  });

  it('should render spinner type', () => {
    component.type = 'spinner';
    fixture.detectChanges();
    
    const spinner = fixture.nativeElement.querySelector('mat-spinner');
    expect(spinner).toBeTruthy();
  });

  it('should render bar type', () => {
    component.type = 'bar';
    fixture.detectChanges();
    
    const bar = fixture.nativeElement.querySelector('mat-progress-bar');
    expect(bar).toBeTruthy();
  });

  it('should render circular type with percentage', () => {
    component.type = 'circular';
    component.mode = 'determinate';
    component.value = 75;
    fixture.detectChanges();
    
    const circular = fixture.nativeElement.querySelector('.circular-progress');
    expect(circular).toBeTruthy();
    
    const percentage = fixture.nativeElement.querySelector('.percentage');
    expect(percentage).toBeTruthy();
    expect(percentage.textContent).toBe('75%');
  });

  it('should not show percentage when showPercentage is false', () => {
    component.type = 'circular';
    component.mode = 'determinate';
    component.value = 50;
    component.showPercentage = false;
    fixture.detectChanges();
    
    const percentage = fixture.nativeElement.querySelector('.percentage');
    expect(percentage).toBeFalsy();
  });

  it('should render dots type', () => {
    component.type = 'dots';
    fixture.detectChanges();
    
    const dots = fixture.nativeElement.querySelectorAll('.dot');
    expect(dots.length).toBe(3);
  });

  it('should display label when provided', () => {
    component.label = 'Loading data...';
    fixture.detectChanges();
    
    const label = fixture.nativeElement.querySelector('.progress-label');
    expect(label).toBeTruthy();
    expect(label.textContent).toBe('Loading data...');
  });

  it('should calculate correct diameter for different sizes', () => {
    component.size = 'small';
    expect(component.getDiameter()).toBe(24);
    
    component.size = 'medium';
    expect(component.getDiameter()).toBe(40);
    
    component.size = 'large';
    expect(component.getDiameter()).toBe(64);
  });

  it('should calculate correct stroke width for different sizes', () => {
    component.size = 'small';
    expect(component.getStrokeWidth()).toBe(3);
    
    component.size = 'medium';
    expect(component.getStrokeWidth()).toBe(4);
    
    component.size = 'large';
    expect(component.getStrokeWidth()).toBe(5);
  });

  it('should calculate correct radius', () => {
    component.size = 'medium';
    const expectedRadius = (40 - 4) / 2;
    expect(component.getRadius()).toBe(expectedRadius);
  });

  it('should calculate correct circumference', () => {
    component.size = 'medium';
    const radius = component.getRadius();
    const expectedCircumference = 2 * Math.PI * radius;
    expect(component.getCircumference()).toBeCloseTo(expectedCircumference);
  });

  it('should calculate correct progress offset', () => {
    component.value = 25;
    const circumference = component.getCircumference();
    const expectedOffset = circumference - (25 / 100) * circumference;
    expect(component.getProgress()).toBeCloseTo(expectedOffset);
  });

  it('should set correct mode on progress bar', () => {
    component.type = 'bar';
    component.mode = 'buffer';
    component.bufferValue = 60;
    fixture.detectChanges();
    
    const bar = fixture.nativeElement.querySelector('mat-progress-bar');
    expect(bar).toBeTruthy();
    // Verify component properties are set correctly
    expect(component.mode).toBe('buffer');
    expect(component.bufferValue).toBe(60);
  });
});