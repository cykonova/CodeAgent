import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ConfigPanel } from './config-panel';

describe('ConfigPanel', () => {
  let component: ConfigPanel;
  let fixture: ComponentFixture<ConfigPanel>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfigPanel]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ConfigPanel);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
