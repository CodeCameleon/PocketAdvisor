import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CategoryService } from '../../core/services/category';

export interface DeleteGlobalCategoryDialogData {
  id: string;
  name: string;
}

interface ValidationProblemDetails {
  errors?: Record<string, string[]>;
}

@Component({
  selector: 'app-delete-global-category-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './delete-global-category-dialog.html',
  styleUrl: './delete-global-category-dialog.css'
})
export class DeleteGlobalCategoryDialog {
  private readonly dialogRef = inject(MatDialogRef<DeleteGlobalCategoryDialog>);
  private readonly data = inject<DeleteGlobalCategoryDialogData>(MAT_DIALOG_DATA);
  private readonly categoryService = inject(CategoryService);

  readonly categoryName = this.data.name;

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onConfirm(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.categoryService.deleteGlobalCategory(this.data.id).subscribe({
      next: () => {
        this.loading.set(false);
        this.dialogRef.close(true);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        this.errorMessage.set(this.resolveErrorMessage(err));
      },
    });
  }

  onCancel(): void {
    this.dialogRef.close(false);
  }

  private resolveErrorMessage(err: HttpErrorResponse): string {
    if (err.status === 404) {
      return 'Category not found. It may have already been deleted.';
    }

    if (err.status === 400) {
      const body = err.error as ValidationProblemDetails | null;
      const messages = body?.errors?.[''] ?? [];

      if (messages.length > 0) {
        return messages[0];
      }
    }

    return 'Failed to delete the category. Please try again.';
  }
}
