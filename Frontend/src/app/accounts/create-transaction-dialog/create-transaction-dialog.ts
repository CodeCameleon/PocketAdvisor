import { Component, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { TransactionService } from '../../core/services/transaction';
import { CategoryService } from '../../core/services/category';
import { ItemService } from '../../core/services/item';
import { ApiErrorService } from '../../core/services/api-error';
import { CategoryResponse } from '../../core/models/category-response';
import { ItemResponse } from '../../core/models/item-response';
import { AccountResponse } from '../../core/models/account-response';
import { Unit, UNIT_LABELS } from '../../core/enums/unit';
import { UnitCategory } from '../../core/enums/unit-category';

export interface CreateTransactionDialogData {
  accountId: string;
  otherAccounts: AccountResponse[];
}

interface UnitOption {
  label: string;
  value: Unit;
}


/** Maps UnitCategory enum values to the Unit hundred-range they occupy.
 *  Uncategorized (1) → units 1–99, Length (2) → 101–199, etc. */
const UNITS_BY_CATEGORY: Record<UnitCategory, UnitOption[]> = (() => {
  const map = {} as Record<UnitCategory, UnitOption[]>;

  for (const cat of Object.values(UnitCategory).filter((v): v is UnitCategory => typeof v === 'number')) {
    map[cat] = Object.values(Unit)
      .filter((v): v is Unit => typeof v === 'number')
      .filter(v => {
        if (cat === UnitCategory.Uncategorized) return v < 100;
        const hundred = (cat - 1) * 100; // Length(2)→100, Mass(3)→200, …
        return v > hundred && v < hundred + 100;
      })
      .map(v => ({ label: UNIT_LABELS[v], value: v }))
      .sort((a, b) => a.label.localeCompare(b.label));
  }

  return map;
})();

const ALL_UNIT_OPTIONS: UnitOption[] = Object.values(Unit)
  .filter((v): v is Unit => typeof v === 'number')
  .map(v => ({ label: UNIT_LABELS[v], value: v }))
  .sort((a, b) => a.label.localeCompare(b.label));

@Component({
  selector: 'app-create-transaction-dialog',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './create-transaction-dialog.html',
  styleUrl: './create-transaction-dialog.css'
})
export class CreateTransactionDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CreateTransactionDialog>);
  private readonly data = inject<CreateTransactionDialogData>(MAT_DIALOG_DATA);
  private readonly transactionService = inject(TransactionService);
  private readonly categoryService = inject(CategoryService);
  private readonly itemService = inject(ItemService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly categories = signal<CategoryResponse[]>([]);
  readonly items = signal<ItemResponse[]>([]);
  readonly otherAccounts: AccountResponse[] = this.data.otherAccounts;
  readonly unitOptions: UnitOption[] = ALL_UNIT_OPTIONS;

  readonly loading = signal(false);
  readonly loadingData = signal(true);
  readonly errorMessage = signal('');

  readonly form = this.fb.nonNullable.group({
    occurredAt: [this.todayIso()],
    categoryId: [''],
    type: ['expense' as 'expense' | 'income'],
    items: this.fb.array([this.buildItemGroup()]),
  });

  get itemsArray(): FormArray {
    return this.form.controls.items;
  }

  ngOnInit(): void {
    this.categoryService.getCategories().subscribe({
      next: (cats) => {
        this.categories.set(cats);
        this.itemService.getItems().subscribe({
          next: (it) => {
            this.items.set(it);
            this.loadingData.set(false);
          },
          error: () => {
            this.errorMessage.set('Failed to load items.');
            this.loadingData.set(false);
          },
        });
      },
      error: () => {
        this.errorMessage.set('Failed to load categories.');
        this.loadingData.set(false);
      },
    });
  }

  /** Returns the unit options appropriate for the item selected at the given row index. */
  unitOptionsForRow(index: number): UnitOption[] {
    const itemId = this.itemsArray.at(index).get('itemId')?.value as string;
    if (!itemId) return ALL_UNIT_OPTIONS;

    const item = this.items().find(i => i.id === itemId);
    if (!item) return ALL_UNIT_OPTIONS;

    return UNITS_BY_CATEGORY[item.unitCategory] ?? ALL_UNIT_OPTIONS;
  }

  /** Called when the item dropdown changes — resets the unit to the first valid option. */
  onItemChange(index: number): void {
    const options = this.unitOptionsForRow(index);
    const unitCtrl = this.itemsArray.at(index).get('unit');

    if (unitCtrl && options.length > 0) {
      unitCtrl.setValue(options[0].value);
    }
  }

  addItem(): void {
    this.itemsArray.push(this.buildItemGroup());
  }

  removeItem(index: number): void {
    if (this.itemsArray.length > 1) {
      this.itemsArray.removeAt(index);
    }
  }

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    const { occurredAt, categoryId, type, items } = this.form.getRawValue();
    const accountId = this.data.accountId;

    const isExpense = type === 'expense';
    const isIncome = type === 'income';
    const isTransfer = !isExpense && !isIncome;

    this.transactionService.createTransaction({
      occurredAt: new Date(occurredAt).toISOString(),
      categoryId: categoryId || null,
      fromAccountId: isExpense || isTransfer ? accountId : null,
      toAccountId: isIncome ? accountId : isTransfer ? type : null,
      items: items.map(i => ({
        itemId: i.itemId || null,
        totalPrice: i.totalPrice,
        amount: i.amount,
        unit: i.unit,
      })),
    }).subscribe({
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

  private buildItemGroup() {
    return this.fb.nonNullable.group({
      itemId: [''],
      totalPrice: [0],
      amount: [1],
      unit: [Unit.Piece],
    });
  }

  private todayIso(): string {
    const d = new Date();
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;
  }
}
