import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CategoryService } from '../../core/services/category';
import { ApiErrorService } from '../../core/services/api-error';

@Component({
  selector: 'app-create-category-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './create-category-dialog.html',
  styleUrl: './create-category-dialog.css'
})
export class CreateCategoryDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CreateCategoryDialog>);
  private readonly categoryService = inject(CategoryService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly form = this.fb.nonNullable.group({
    name: [''],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.categoryService.createPersonalCategory(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.dialogRef.close(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.apiErrorService.applyErrors(err, this.form));
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }
}
