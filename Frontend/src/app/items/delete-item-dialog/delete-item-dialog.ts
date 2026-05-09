import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ItemService } from '../../core/services/item';

export interface DeleteItemDialogData {
  id: string;
  name: string;
}

@Component({
  selector: 'app-delete-item-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './delete-item-dialog.html',
  styleUrl: './delete-item-dialog.css'
})
export class DeleteItemDialog {
  private readonly dialogRef = inject(MatDialogRef<DeleteItemDialog>);
  private readonly data = inject<DeleteItemDialogData>(MAT_DIALOG_DATA);
  private readonly itemService = inject(ItemService);

  readonly itemName = this.data.name;

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onConfirm(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.itemService.deleteItem(this.data.id).subscribe({
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
      return 'Item not found. It may have already been deleted.';
    }

    return 'Failed to delete the item. Please try again.';
  }
}
