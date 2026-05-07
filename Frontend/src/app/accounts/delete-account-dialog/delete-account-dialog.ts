import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AccountService } from '../../core/services/account';

export interface DeleteAccountDialogData {
  id: string;
  name: string;
}

@Component({
  selector: 'app-delete-account-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './delete-account-dialog.html',
  styleUrl: './delete-account-dialog.css'
})
export class DeleteAccountDialog {
  private readonly dialogRef = inject(MatDialogRef<DeleteAccountDialog>);
  private readonly data = inject<DeleteAccountDialogData>(MAT_DIALOG_DATA);
  private readonly accountService = inject(AccountService);

  readonly accountName = this.data.name;

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onConfirm(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.accountService.deleteAccount(this.data.id).subscribe({
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
      return 'Account not found. It may have already been deleted.';
    }

    return 'Failed to delete the account. Please try again.';
  }
}
