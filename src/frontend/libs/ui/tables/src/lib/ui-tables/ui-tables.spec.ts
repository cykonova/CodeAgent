import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UiTables } from './ui-tables';

describe('UiTables', () => {
  let component: UiTables;
  let fixture: ComponentFixture<UiTables>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [UiTables],
    }).compileComponents();

    fixture = TestBed.createComponent(UiTables);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
