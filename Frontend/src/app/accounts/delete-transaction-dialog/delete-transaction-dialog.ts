import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DatePipe } from '@angular/common';

import { TransactionService } from '../../core/services/transaction';

export interface DeleteTransactionDialogData {
  id: string;
  occurredAt: string;
  itemCount: number;
}

@Component({
  selector: 'app-delete-transaction-dialog',
  imports: [
    DatePipe,
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './delete-transaction-dialog.html',
  styleUrl: './delete-transaction-dialog.css'
})
export class DeleteTransactionDialog {
  private readonly dialogRef = inject(MatDialogRef<DeleteTransactionDialog>);
  private readonly data = inject<DeleteTransactionDialogData>(MAT_DIALOG_DATA);
  private readonly transactionService = inject(TransactionService);

  readonly occurredAt = this.data.occurredAt;
  readonly itemCount = this.data.itemCount;

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onConfirm(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.transactionService.deleteTransaction(this.data.id).subscribe({
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
      return 'Transaction not found. It may have already been deleted.';
    }

    return 'Failed to delete the transaction. Please try again.';
  }
}
