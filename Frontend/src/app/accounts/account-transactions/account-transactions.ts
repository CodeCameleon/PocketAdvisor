import { Component, inject, OnInit, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { animate, style, transition, trigger } from '@angular/animations';

import { TransactionService } from '../../core/services/transaction';
import { AccountService } from '../../core/services/account';
import { ItemService } from '../../core/services/item';
import { TransactionResponse } from '../../core/models/transaction-response';
import { AccountResponse } from '../../core/models/account-response';
import { CurrencyCode } from '../../core/enums/currency-code';
import { Unit, UNIT_LABELS } from '../../core/enums/unit';
import { CreateTransactionDialog } from '../create-transaction-dialog/create-transaction-dialog';
import { DeleteTransactionDialog } from '../delete-transaction-dialog/delete-transaction-dialog';

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
  styleUrl: './account-transactions.css',
  animations: [
    trigger('expandCollapse', [
      transition(':enter', [
        style({ height: 0, opacity: 0, overflow: 'hidden' }),
        animate('180ms ease-out', style({ height: '*', opacity: 1 }))
      ]),
      transition(':leave', [
        style({ overflow: 'hidden' }),
        animate('150ms ease-in', style({ height: 0, opacity: 0 }))
      ])
    ])
  ]
})
export class AccountTransactions implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly transactionService = inject(TransactionService);
  private readonly accountService = inject(AccountService);
  private readonly itemService = inject(ItemService);

  readonly account = signal<AccountResponse | null>(null);
  readonly transactions = signal<TransactionResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly expandedTxId = signal<string | null>(null);
  readonly deletingItemId = signal<string | null>(null);

  private allAccounts: AccountResponse[] = [];
  private itemNames = new Map<string, string>();

  private accountId = '';

  ngOnInit(): void {
    this.accountId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadData();
  }

  goBack(): void {
    this.router.navigate(['/accounts']);
  }

  toggleExpand(txId: string): void {
    this.expandedTxId.update(current => (current === txId ? null : txId));
  }

  deleteItem(tx: TransactionResponse, itemId: string, event: MouseEvent): void {
    event.stopPropagation();
    if (tx.items.length <= 1 || this.deletingItemId()) return;

    this.deletingItemId.set(itemId);
    this.transactionService.deleteTransactionItem(tx.id, itemId).subscribe({
      next: () => {
        this.transactions.update(txs =>
          txs.map(t =>
            t.id === tx.id
              ? { ...t, items: t.items.filter(i => i.itemId !== itemId) }
              : t
          )
        );
        this.deletingItemId.set(null);

        // Refresh account balance
        this.accountService.getAccounts().subscribe({
          next: accounts => {
            const found = accounts.find(a => a.id === this.accountId) ?? null;
            this.account.set(found);
          },
        });
      },
      error: () => {
        this.deletingItemId.set(null);
      },
    });
  }

  openDeleteDialog(tx: TransactionResponse, event: MouseEvent): void {
    event.stopPropagation();

    const ref = this.dialog.open(DeleteTransactionDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: {
        id: tx.id,
        occurredAt: tx.occurredAt,
        itemCount: tx.items.length,
      },
    });

    ref.afterClosed().subscribe((deleted: boolean) => {
      if (deleted) {
        this.transactions.update(txs => txs.filter(t => t.id !== tx.id));

        // Refresh account balance
        this.accountService.getAccounts().subscribe({
          next: accounts => {
            const found = accounts.find(a => a.id === this.accountId) ?? null;
            this.account.set(found);
          },
        });
      }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateTransactionDialog, {
      width: '620px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: {
        accountId: this.accountId,
        otherAccounts: this.allAccounts.filter(a => a.id !== this.accountId)
      },
    });

    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.loadData();
      }
    });
  }

  /** Converts a numeric CurrencyCode enum value to its ISO 4217 alpha string (e.g. 840 -> "USD"). */
  currencyAlpha(code: CurrencyCode): string {
    return CurrencyCode[code].toUpperCase();
  }

  /** Returns the human-readable label for a Unit enum value (e.g. 202 → "Gram"). */
  unitLabel(unit: Unit): string {
    return UNIT_LABELS[unit] ?? String(unit);
  }

  /** Returns the name of the item with the given id, or a fallback if not loaded yet. */
  itemName(itemId: string): string {
    return this.itemNames.get(itemId) ?? 'Unknown item';
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

    forkJoin({
      accounts: this.accountService.getAccounts(),
      items: this.itemService.getItems(),
    }).subscribe({
      next: ({ accounts, items }) => {
        this.allAccounts = accounts;
        this.account.set(accounts.find(a => a.id === this.accountId) ?? null);
        this.itemNames = new Map(items.map(i => [i.id, i.name]));

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
