import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { By } from '@angular/platform-browser';
import { CardComponent } from './card.component';

describe('CardComponent', () => {
  let component: CardComponent;
  let fixture: ComponentFixture<CardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CardComponent, NoopAnimationsModule]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Title and Subtitle', () => {
    it('should display title when provided', () => {
      component.title = 'Test Title';
      fixture.detectChanges();
      
      const titleElement = fixture.debugElement.query(By.css('mat-card-title'));
      expect(titleElement).toBeTruthy();
      expect(titleElement.nativeElement.textContent).toContain('Test Title');
    });

    it('should display subtitle when provided', () => {
      component.subtitle = 'Test Subtitle';
      fixture.detectChanges();
      
      const subtitleElement = fixture.debugElement.query(By.css('mat-card-subtitle'));
      expect(subtitleElement).toBeTruthy();
      expect(subtitleElement.nativeElement.textContent).toContain('Test Subtitle');
    });

    it('should not display header when neither title nor subtitle is provided', () => {
      const headerElement = fixture.debugElement.query(By.css('mat-card-header'));
      expect(headerElement).toBeFalsy();
    });

    it('should display header when only title is provided', () => {
      component.title = 'Only Title';
      fixture.detectChanges();
      
      const headerElement = fixture.debugElement.query(By.css('mat-card-header'));
      expect(headerElement).toBeTruthy();
    });
  });

  describe('Elevation Styles', () => {
    it('should apply raised elevation by default', () => {
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('elevation-raised')).toBeTruthy();
    });

    it('should apply flat elevation class', () => {
      component.elevation = 'flat';
      fixture.detectChanges();
      
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('elevation-flat')).toBeTruthy();
    });

    it('should apply stroked elevation class', () => {
      component.elevation = 'stroked';
      fixture.detectChanges();
      
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('elevation-stroked')).toBeTruthy();
    });
  });

  describe('Padding Styles', () => {
    it('should apply medium padding by default', () => {
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('padding-medium')).toBeTruthy();
    });

    it('should apply none padding class', () => {
      component.padding = 'none';
      fixture.detectChanges();
      
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('padding-none')).toBeTruthy();
    });

    it('should apply small padding class', () => {
      component.padding = 'small';
      fixture.detectChanges();
      
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('padding-small')).toBeTruthy();
    });

    it('should apply large padding class', () => {
      component.padding = 'large';
      fixture.detectChanges();
      
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('padding-large')).toBeTruthy();
    });
  });

  describe('Clickable State', () => {
    it('should not have clickable class by default', () => {
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('clickable')).toBeFalsy();
    });

    it('should apply clickable class when clickable is true', () => {
      component.clickable = true;
      fixture.detectChanges();
      
      const cardElement = fixture.debugElement.query(By.css('mat-card'));
      expect(cardElement.nativeElement.classList.contains('clickable')).toBeTruthy();
    });
  });

  describe('Tools Section', () => {
    it('should not display tools by default', () => {
      const toolsElement = fixture.debugElement.query(By.css('.card-header-tools'));
      expect(toolsElement).toBeFalsy();
    });

    it('should display tools when showTools is true', () => {
      component.showTools = true;
      fixture.detectChanges();
      
      const toolsElement = fixture.debugElement.query(By.css('.card-header-tools'));
      expect(toolsElement).toBeTruthy();
    });

    it('should display header for tools even without title or subtitle', () => {
      component.showTools = true;
      component.title = undefined;
      component.subtitle = undefined;
      fixture.detectChanges();
      
      const headerElement = fixture.debugElement.query(By.css('mat-card-header'));
      expect(headerElement).toBeTruthy();
      
      const toolsElement = fixture.debugElement.query(By.css('.card-header-tools'));
      expect(toolsElement).toBeTruthy();
    });
  });

  describe('Actions Section', () => {
    it('should not display actions by default', () => {
      const actionsElement = fixture.debugElement.query(By.css('mat-card-actions'));
      expect(actionsElement).toBeFalsy();
    });

    it('should display actions when showActions is true', () => {
      component.showActions = true;
      fixture.detectChanges();
      
      const actionsElement = fixture.debugElement.query(By.css('mat-card-actions'));
      expect(actionsElement).toBeTruthy();
    });

    it('should align actions to end by default', () => {
      component.showActions = true;
      fixture.detectChanges();
      
      const actionsElement = fixture.debugElement.query(By.css('mat-card-actions'));
      expect(actionsElement.nativeElement.getAttribute('align')).toBe('end');
    });

    it('should align actions to start when specified', () => {
      component.showActions = true;
      component.actionsAlign = 'start';
      fixture.detectChanges();
      
      const actionsElement = fixture.debugElement.query(By.css('mat-card-actions'));
      expect(actionsElement.nativeElement.getAttribute('align')).toBe('start');
    });
  });

  describe('Content Projection', () => {
    it('should project content into card content area', () => {
      fixture = TestBed.createComponent(CardComponent);
      fixture.nativeElement.innerHTML = '<p>Test Content</p>';
      fixture.detectChanges();
      
      const contentElement = fixture.debugElement.query(By.css('mat-card-content'));
      expect(contentElement).toBeTruthy();
    });
  });

  describe('Card Classes', () => {
    it('should return correct classes object', () => {
      component.elevation = 'flat';
      component.padding = 'large';
      component.clickable = true;
      
      const classes = component.cardClasses;
      
      expect(classes['elevation-flat']).toBeTruthy();
      expect(classes['padding-large']).toBeTruthy();
      expect(classes['clickable']).toBeTruthy();
    });
  });
});