import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { TransactionService } from '../../core/services/transaction';
import { AccountService } from '../../core/services/account';
import { TransactionResponse } from '../../core/models/transaction-response';
import { AccountResponse } from '../../core/models/account-response';
import { CurrencyCode } from '../../core/enums/currency-code';

@Component({
  selector: 'app-account-transactions',
  imports: [
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './account-transactions.html',
  styleUrl: './account-transactions.css'
})
export class AccountTransactions implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly transactionService = inject(TransactionService);
  private readonly accountService = inject(AccountService);

  readonly account = signal<AccountResponse | null>(null);
  readonly transactions = signal<TransactionResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  private accountId = '';

  ngOnInit(): void {
    this.accountId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadData();
  }

  goBack(): void {
    this.router.navigate(['/accounts']);
  }

  /** Converts a numeric CurrencyCode enum value to its ISO 4217 alpha string (e.g. 840 -> "USD"). */
  currencyAlpha(code: CurrencyCode): string {
    return CurrencyCode[code].toUpperCase();
  }

  /** Returns the net amount of a transaction relative to this account.
   *  Positive when money flows in (toAccountId matches), negative when money flows out. */
  transactionAmount(tx: TransactionResponse): number {
    const total = tx.items.reduce((sum, item) => sum + item.totalPrice, 0);
    return tx.toAccountId === this.accountId ? total : -total;
  }

  private loadData(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.accountService.getAccounts().subscribe({
      next: (accounts) => {
        const found = accounts.find(a => a.id === this.accountId) ?? null;
        this.account.set(found);

        this.transactionService.getTransactions(this.accountId).subscribe({
          next: (txs) => {
            const sorted = [...txs].sort(
              (a, b) => new Date(b.occurredAt).getTime() - new Date(a.occurredAt).getTime()
            );
            this.transactions.set(sorted);
            this.loading.set(false);
          },
          error: () => {
            this.errorMessage.set('Failed to load transactions. Please try again.');
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.errorMessage.set('Failed to load account details. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
