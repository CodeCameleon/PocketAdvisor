import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CategoryService } from '../../core/services/category';
import { ApiErrorService } from '../../core/services/api-error';

export interface UpdateCategoryNameDialogData {
  id: string;
  name: string;
}

@Component({
  selector: 'app-update-category-name-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './update-category-name-dialog.html',
  styleUrl: './update-category-name-dialog.css'
})
export class UpdateCategoryNameDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<UpdateCategoryNameDialog>);
  private readonly data = inject<UpdateCategoryNameDialogData>(MAT_DIALOG_DATA);
  private readonly categoryService = inject(CategoryService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly form = this.fb.nonNullable.group({
    name: [this.data.name],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.categoryService.updatePersonalCategoryName(this.data.id, this.form.getRawValue()).subscribe({
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
