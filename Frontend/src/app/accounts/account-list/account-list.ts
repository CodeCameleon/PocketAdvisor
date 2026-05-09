import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { AccountService } from '../../core/services/account';
import { AccountResponse } from '../../core/models/account-response';
import { CurrencyCode } from '../../core/enums/currency-code';
import { CreateAccountDialog } from '../create-account-dialog/create-account-dialog';
import { DeleteAccountDialog } from '../delete-account-dialog/delete-account-dialog';
import { UpdateAccountNameDialog } from '../update-account-name-dialog/update-account-name-dialog';

@Component({
  selector: 'app-account-list',
  imports: [
    CurrencyPipe,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './account-list.html',
  styleUrl: './account-list.css'
})
export class AccountList implements OnInit {
  private readonly accountService = inject(AccountService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly accounts = signal<AccountResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadAccounts();
  }

  openTransactions(account: AccountResponse): void {
    this.router.navigate(['/accounts', account.id, 'transactions']);
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateAccountDialog, {
      width: '520px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.loadAccounts();
      }
    });
  }

  openRenameDialog(account: AccountResponse): void {
    const ref = this.dialog.open(UpdateAccountNameDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { id: account.id, name: account.name },
    });

    ref.afterClosed().subscribe((updated: boolean) => {
      if (updated) {
        this.loadAccounts();
      }
    });
  }

  openDeleteDialog(account: AccountResponse): void {
    const ref = this.dialog.open(DeleteAccountDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { id: account.id, name: account.name },
    });

    ref.afterClosed().subscribe((deleted: boolean) => {
      if (deleted) {
        this.loadAccounts();
      }
    });
  }

  /** Converts a numeric CurrencyCode enum value to its ISO 4217 alpha string (e.g. 840 -> "USD"). */
  currencyAlpha(code: CurrencyCode): string {
    return CurrencyCode[code].toUpperCase();
  }

  private loadAccounts(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        this.accounts.set(accounts);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load accounts. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
