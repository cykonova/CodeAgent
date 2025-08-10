import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';

export interface FormFieldConfig {
  key: string;
  label: string;
  type: 'text' | 'email' | 'password' | 'number' | 'select' | 'checkbox' | 'radio' | 'date' | 'textarea' | 'toggle';
  value?: any;
  options?: Array<{ label: string; value: any }>;
  validators?: any[];
  placeholder?: string;
  hint?: string;
  disabled?: boolean;
  required?: boolean;
  min?: number;
  max?: number;
  rows?: number;
  icon?: string;
}

@Component({
  selector: 'app-dynamic-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatCheckboxModule,
    MatRadioModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule
  ],
  templateUrl: './dynamic-form.component.html',
  styleUrls: ['./dynamic-form.component.scss']
})
export class DynamicFormComponent implements OnInit {
  @Input() fields: FormFieldConfig[] = [];
  @Input() submitLabel = 'Submit';
  @Input() cancelLabel = 'Cancel';
  @Input() showCancel = true;
  @Output() formSubmit = new EventEmitter<any>();
  @Output() formCancel = new EventEmitter<void>();
  
  form!: FormGroup;
  
  constructor(private fb: FormBuilder) {}
  
  ngOnInit(): void {
    this.createForm();
  }
  
  private createForm(): void {
    const group: any = {};
    
    this.fields.forEach(field => {
      const validators = this.getValidators(field);
      group[field.key] = [
        { value: field.value || '', disabled: field.disabled },
        validators
      ];
    });
    
    this.form = this.fb.group(group);
  }
  
  private getValidators(field: FormFieldConfig): any[] {
    const validators = field.validators || [];
    
    if (field.required) {
      validators.push(Validators.required);
    }
    
    if (field.type === 'email') {
      validators.push(Validators.email);
    }
    
    if (field.min !== undefined) {
      validators.push(Validators.min(field.min));
    }
    
    if (field.max !== undefined) {
      validators.push(Validators.max(field.max));
    }
    
    return validators;
  }
  
  onSubmit(): void {
    if (this.form.valid) {
      this.formSubmit.emit(this.form.value);
    } else {
      this.markFormGroupTouched(this.form);
    }
  }
  
  onCancel(): void {
    this.formCancel.emit();
  }
  
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();
      
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
  
  getErrorMessage(field: FormFieldConfig): string {
    const control = this.form.get(field.key);
    
    if (control?.hasError('required')) {
      return `${field.label} is required`;
    }
    
    if (control?.hasError('email')) {
      return 'Please enter a valid email';
    }
    
    if (control?.hasError('min')) {
      return `Minimum value is ${field.min}`;
    }
    
    if (control?.hasError('max')) {
      return `Maximum value is ${field.max}`;
    }
    
    return '';
  }
}