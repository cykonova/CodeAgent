import { Component, Input, Output, EventEmitter, forwardRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-form-field',
  standalone: true,
  imports: [CommonModule, MatFormFieldModule, MatInputModule, MatIconModule, FormsModule],
  templateUrl: './form-field.html',
  styleUrl: './form-field.scss',
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => FormFieldComponent),
    multi: true
  }]
})
export class FormFieldComponent implements ControlValueAccessor {
  @Input() label: string = '';
  @Input() placeholder: string = '';
  @Input() type: string = 'text';
  @Input() hint?: string;
  @Input() error?: string;
  @Input() prefixIcon?: string;
  @Input() suffixIcon?: string;
  @Input() appearance: 'fill' | 'outline' = 'outline';
  @Input() readonly: boolean = false;
  @Input() disabled: boolean = false;
  @Input() required: boolean = false;
  @Input() fullWidth: boolean = true;
  
  @Output() valueChange = new EventEmitter<any>();
  @Output() blur = new EventEmitter<void>();
  @Output() focus = new EventEmitter<void>();
  
  value: any = '';
  
  private onChange: (value: any) => void = () => {};
  private onTouched: () => void = () => {};
  
  writeValue(value: any): void {
    this.value = value;
  }
  
  registerOnChange(fn: any): void {
    this.onChange = fn;
  }
  
  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }
  
  setDisabledState(isDisabled: boolean): void {
    this.disabled = isDisabled;
  }
  
  onValueChange(value: any) {
    this.value = value;
    this.onChange(value);
    this.valueChange.emit(value);
  }
  
  onBlur() {
    this.onTouched();
    this.blur.emit();
  }
  
  onFocus() {
    this.focus.emit();
  }
}