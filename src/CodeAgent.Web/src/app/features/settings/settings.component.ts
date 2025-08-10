import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatTabsModule],
  template: `
    <div class="settings-container">
      <h1 class="page-title">Settings</h1>
      
      <mat-card>
        <mat-card-content>
          <mat-tab-group>
            <mat-tab label="General">
              <div class="tab-content">
                <p>General settings will be implemented here.</p>
              </div>
            </mat-tab>
            <mat-tab label="Providers">
              <div class="tab-content">
                <p>Provider configuration will be implemented here.</p>
              </div>
            </mat-tab>
            <mat-tab label="Security">
              <div class="tab-content">
                <p>Security settings will be implemented here.</p>
              </div>
            </mat-tab>
          </mat-tab-group>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .settings-container {
      max-width: var(--container-lg);
      margin: 0 auto;
    }
    
    .page-title {
      font-size: 32px;
      font-weight: var(--font-weight-light);
      margin-bottom: var(--spacing-lg);
      color: var(--mat-on-surface);
    }
    
    .tab-content {
      padding: var(--spacing-lg);
    }
  `]
})
export class SettingsComponent {}