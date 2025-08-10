# Theme Guidelines

## Overview
This document outlines the theming standards and design guidelines for the Code Agent application.

## Form Fields

### Standard Appearance
**All form fields MUST use the `outline` appearance by default.**

- The outline appearance is configured globally in `app.config.ts`
- DO NOT specify `appearance="fill"` unless there's a specific design requirement
- The outline style provides better visual consistency and accessibility

### Usage Examples

```html
<!-- CORRECT - Uses outline by default -->
<mat-form-field>
  <mat-label>Username</mat-label>
  <input matInput>
</mat-form-field>

<!-- AVOID - Don't specify appearance unless needed -->
<mat-form-field appearance="fill">
  <mat-label>Username</mat-label>
  <input matInput>
</mat-form-field>
```

## Buttons

### Border Radius
Buttons use a squared appearance with minimal border radius (2px) for a modern, professional look.

- Regular buttons: 2px border radius
- Icon buttons: Remain circular (50% border radius)
- FAB buttons: 4px border radius

### Button Types
- Use `mat-raised-button` for primary actions
- Use `mat-stroked-button` for secondary actions
- Use `mat-flat-button` sparingly for special emphasis
- Use `mat-icon-button` for icon-only actions

## Spacing

Follow the 8px grid system using CSS custom properties:
- `--spacing-xs`: 4px
- `--spacing-sm`: 8px
- `--spacing-md`: 16px
- `--spacing-lg`: 24px
- `--spacing-xl`: 32px
- `--spacing-xxl`: 48px

**NEVER hardcode spacing values.** Always use the CSS variables or utility classes.

## Colors

### Text Colors
- Primary text: `var(--text-primary)`
- Secondary text: `var(--text-secondary)`
- Disabled text: `var(--text-disabled)`
- Hint text: `var(--text-hint)`

### Background Colors
- Primary background: `var(--background-primary)`
- Secondary background: `var(--background-secondary)`
- Tertiary background: `var(--background-tertiary)`

### Material Theme Colors
- Primary: Azure blue palette
- Accent/Tertiary: Blue palette
- Warn: Red palette (default)

## Cards and Surfaces

### Border Radius
- Cards: `--radius-md` (8px)
- Small elements: `--radius-sm` (4px)
- Large cards: `--radius-lg` (16px)

### Elevation
Use shadow utilities for depth:
- `.shadow-sm`: Subtle elevation
- `.shadow-md`: Standard card elevation
- `.shadow-lg`: Emphasized elevation
- `.shadow-xl`: Maximum elevation

## Typography

Use Material Typography classes:
- Headlines: `.mat-headline-1` through `.mat-headline-6`
- Body text: `.mat-body-1`, `.mat-body-2`
- Captions: `.mat-caption`
- Buttons: `.mat-button`

## Responsive Design

- Mobile-first approach
- Use responsive spacing that scales down on mobile
- Maintain touch targets of at least 48px on mobile devices
- Use breakpoint mixins for responsive styles

## Dark Theme Support

All colors and styles must work in both light and dark themes:
- Test all components in both themes
- Use theme-aware variables, never hardcode colors
- Ensure sufficient contrast ratios (WCAG AA compliance)

## Best Practices

1. **Never hardcode values** - Use theme variables
2. **Use utility classes** - Leverage the provided utility classes
3. **Test in both themes** - Ensure components work in light and dark modes
4. **Follow Material Design** - Respect Material Design principles
5. **Maintain consistency** - Use the same patterns throughout the application