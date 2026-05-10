import {
  AfterViewInit,
  Component,
  ElementRef,
  inject,
  OnDestroy,
  OnInit,
  signal,
  ViewChild,
} from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ItemService } from '../../core/services/item';
import { TransactionService } from '../../core/services/transaction';
import { AccountService } from '../../core/services/account';
import { ItemResponse } from '../../core/models/item-response';
import { TransactionResponse } from '../../core/models/transaction-response';
import { AccountResponse } from '../../core/models/account-response';
import { CurrencyCode } from '../../core/enums/currency-code';
import { Unit, UNIT_LABELS } from '../../core/enums/unit';

// Chart.js is loaded globally via index.html CDN script
declare const Chart: any;

@Component({
  selector: 'app-item-detail',
  imports: [
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './item-detail.html',
  styleUrl: './item-detail.css'
})
export class ItemDetail implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('chartCanvas') private chartCanvas!: ElementRef<HTMLCanvasElement>;

  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly itemService = inject(ItemService);
  private readonly transactionService = inject(TransactionService);
  private readonly accountService = inject(AccountService);

  readonly item = signal<ItemResponse | null>(null);
  readonly transactions = signal<TransactionResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');
  readonly expandedTxId = signal<string | null>(null);

  /** ISO 4217 alpha code (e.g. "USD") used for the grand total header stat.
   *  Taken from the first transaction's account; null when mixed or unavailable. */
  readonly headerCurrencyAlpha = signal<string | null>(null);

  private itemId = '';
  private accountsById = new Map<string, AccountResponse>();
  private chartInstance: any = null;
  private viewReady = false;
  private dataReady = false;

  ngOnInit(): void {
    this.itemId = this.route.snapshot.paramMap.get('id') ?? '';
    this.loadData();
  }

  ngAfterViewInit(): void {
    this.viewReady = true;

    if (this.dataReady) {
      this.renderChart();
    }
  }

  ngOnDestroy(): void {
    this.chartInstance?.destroy();
  }

  goBack(): void {
    this.router.navigate(['/items']);
  }

  toggleExpand(txId: string): void {
    this.expandedTxId.update(current => (current === txId ? null : txId));
  }

  unitLabel(unit: Unit): string {
    return UNIT_LABELS[unit] ?? String(unit);
  }

  /** Returns the ISO 4217 alpha currency code for a transaction (e.g. "USD"). */
  currencyAlpha(tx: TransactionResponse): string {
    const accountId = tx.fromAccountId ?? tx.toAccountId;
    const account = accountId ? this.accountsById.get(accountId) : undefined;
    if (!account) return 'USD';
    return CurrencyCode[account.currencyCode].toUpperCase();
  }

  /** Returns the total price paid for this item within a given transaction. */
  itemTotalForTx(tx: TransactionResponse): number {
    return tx.items.filter(i => i.itemId === this.itemId).reduce((sum, i) => sum + i.totalPrice, 0);
  }

  /** Grand total across all transactions (only meaningful when all share the same currency). */
  get grandTotal(): number {
    return this.transactions().reduce(
      (sum, tx) => sum + this.itemTotalForTx(tx),
      0
    );
  }

  private loadData(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    forkJoin({
      items: this.itemService.getItems(),
      accounts: this.accountService.getAccounts(),
      transactions: this.transactionService.getTransactionsByItem(this.itemId),
    }).subscribe({
      next: ({ items, accounts, transactions }) => {
        this.item.set(items.find(i => i.id === this.itemId) ?? null);
        this.accountsById = new Map(accounts.map(a => [a.id, a]));

        const sorted = [...transactions].sort(
          (a, b) =>
            new Date(a.occurredAt).getTime() - new Date(b.occurredAt).getTime()
        );
        this.transactions.set(sorted);

        // Determine header currency: show it only when all transactions share the same one
        const currencies = new Set(sorted.map(tx => this.currencyAlpha(tx)));
        this.headerCurrencyAlpha.set(currencies.size === 1 ? [...currencies][0] : null);

        this.loading.set(false);
        this.dataReady = true;

        if (this.viewReady) {
          setTimeout(() => this.renderChart(), 0);
        }
      },
      error: () => {
        this.errorMessage.set('Failed to load item details. Please try again.');
        this.loading.set(false);
      },
    });
  }

  private renderChart(): void {
    if (!this.chartCanvas?.nativeElement) return;
    if (this.transactions().length === 0) return;

    // Aggregate spend per date (multiple transactions on the same day are summed)
    const spendByDate = new Map<string, number>();
    for (const tx of this.transactions()) {
      const day = tx.occurredAt.substring(0, 10); // "YYYY-MM-DD"
      const spent = this.itemTotalForTx(tx);
      spendByDate.set(day, (spendByDate.get(day) ?? 0) + spent);
    }

    const sorted = [...spendByDate.entries()].sort(([a], [b]) =>
      a.localeCompare(b)
    );

    const labels = sorted.map(([date]) => {
      const [year, month, day] = date.split('-').map(Number);
      return new Date(year, month - 1, day).toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
      });
    });
    const data = sorted.map(([, amount]) => amount);

    this.chartInstance?.destroy();
    this.chartInstance = new Chart(this.chartCanvas.nativeElement, {
      type: 'line',
      data: {
        labels,
        datasets: [
          {
            label: 'Amount spent',
            data,
            fill: true,
            tension: 0.35,
            borderColor: '#2355D8',
            backgroundColor: 'rgba(35, 85, 216, 0.08)',
            pointBackgroundColor: '#2355D8',
            pointBorderColor: '#fff',
            pointBorderWidth: 2,
            pointRadius: 5,
            pointHoverRadius: 7,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            callbacks: {
              label: (ctx: any) => ` ${ctx.parsed.y.toFixed(2)}`,
            },
          },
        },
        scales: {
          x: {
            grid: { color: 'rgba(0,0,0,0.05)' },
            ticks: {
              color: '#6d7490',
              font: { size: 12 },
              maxRotation: 40,
            },
          },
          y: {
            beginAtZero: true,
            grid: { color: 'rgba(0,0,0,0.05)' },
            ticks: {
              color: '#6d7490',
              font: { size: 12 },
              callback: (value: number) => value.toFixed(2),
            },
          },
        },
      },
    });
  }
}
