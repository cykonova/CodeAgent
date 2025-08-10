import { Pipe, PipeTransform, inject, ChangeDetectorRef, OnDestroy } from '@angular/core';
import { TranslationService } from './translation.service';
import { Subscription } from 'rxjs';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false
})
export class TranslatePipe implements PipeTransform, OnDestroy {
  private translationService = inject(TranslationService);
  private changeDetector = inject(ChangeDetectorRef);
  private subscription?: Subscription;
  private lastKey?: string;
  private lastValue?: string;
  
  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
  
  transform(key: string, ...args: any[]): string {
    if (!key) {
      return '';
    }
    
    // Return cached value if key hasn't changed
    if (key === this.lastKey && this.lastValue) {
      return this.lastValue;
    }
    
    // Subscribe to translation changes if not already subscribed
    if (!this.subscription) {
      this.subscription = this.translationService.translations$.subscribe(() => {
        this.lastValue = undefined;
        this.changeDetector.markForCheck();
      });
    }
    
    this.lastKey = key;
    this.lastValue = this.translationService.translate(key);
    
    // Replace placeholders with arguments if provided
    if (args.length > 0 && this.lastValue) {
      args.forEach((arg, index) => {
        this.lastValue = this.lastValue!.replace(`{${index}}`, arg);
      });
    }
    
    return this.lastValue;
  }
}