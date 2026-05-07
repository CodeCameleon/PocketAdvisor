import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { AccountService } from '../../core/services/account';
import { ApiErrorService } from '../../core/services/api-error';
import { CurrencyCode } from '../../core/enums/currency-code';

/** Display label + enum value pair shown in the currency dropdown. */
interface CurrencyOption {
  label: string;
  value: CurrencyCode;
}

/** Commonly used currencies shown at the top of the list. */
const PINNED_CURRENCY_CODES: CurrencyCode[] = [
  CurrencyCode.Aud,
  CurrencyCode.Cad,
  CurrencyCode.Eur,
  CurrencyCode.Gbp,
  CurrencyCode.Huf,
  CurrencyCode.Usd
];

/** Builds a human-readable "USD – US Dollar" label from a CurrencyCode enum member. */
function buildCurrencyLabel(key: string, value: CurrencyCode): string {
  const alpha = key.toUpperCase();

  try {
    const name = new Intl.DisplayNames(['en'], { type: 'currency' }).of(alpha) ?? alpha;
    return `${alpha} – ${name}`;
  } catch {
    return alpha;
  }
}

/** All CurrencyCode entries as { label, value } pairs, sorted alphabetically. */
function buildAllOptions(): CurrencyOption[] {
  return Object.entries(CurrencyCode).filter(([, v]) => typeof v === 'number').map(([key, value]) =>
    ({ label: buildCurrencyLabel(key, value as CurrencyCode), value: value as CurrencyCode })
  ).sort((a, b) => a.label.localeCompare(b.label));
}

@Component({
  selector: 'app-create-account-dialog',
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
  templateUrl: './create-account-dialog.html',
  styleUrl: './create-account-dialog.css'
})
export class CreateAccountDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CreateAccountDialog>);
  private readonly accountService = inject(AccountService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly pinnedOptions: CurrencyOption[] = PINNED_CURRENCY_CODES.map(code => {
    const key = CurrencyCode[code];
    return { label: buildCurrencyLabel(key, code), value: code };
  });

  readonly allOptions: CurrencyOption[] = buildAllOptions();

  readonly form = this.fb.nonNullable.group({
    name: [''],
    balance: [0],
    currencyCode: [CurrencyCode.Huf],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.accountService.createAccount(this.form.getRawValue()).subscribe({
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
}
