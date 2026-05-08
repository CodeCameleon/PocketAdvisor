import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CategoryService } from '../../core/services/category';

export interface DeleteCategoryDialogData {
  id: string;
  name: string;
}

@Component({
  selector: 'app-delete-category-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './delete-category-dialog.html',
  styleUrl: './delete-category-dialog.css'
})
export class DeleteCategoryDialog {
  private readonly dialogRef = inject(MatDialogRef<DeleteCategoryDialog>);
  private readonly data = inject<DeleteCategoryDialogData>(MAT_DIALOG_DATA);
  private readonly categoryService = inject(CategoryService);

  readonly categoryName = this.data.name;

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onConfirm(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.categoryService.deletePersonalCategory(this.data.id).subscribe({
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

    return 'Failed to delete the category. Please try again.';
  }
}
