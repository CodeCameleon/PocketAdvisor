import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

import { ItemService } from '../../core/services/item';
import { ApiErrorService } from '../../core/services/api-error';
import { UnitCategory } from '../../core/enums/unit-category';

/** Display label + enum value pair shown in the unit category dropdown. */
interface UnitCategoryOption {
  label: string;
  value: UnitCategory;
}

const UNIT_CATEGORY_OPTIONS: UnitCategoryOption[] = [
  { label: 'Uncategorized', value: UnitCategory.Uncategorized },
  { label: 'Area',          value: UnitCategory.Area          },
  { label: 'Data Size',     value: UnitCategory.DataSize      },
  { label: 'Energy',        value: UnitCategory.Energy        },
  { label: 'Length',        value: UnitCategory.Length        },
  { label: 'Mass',          value: UnitCategory.Mass          },
  { label: 'Time',          value: UnitCategory.Time          },
  { label: 'Volume',        value: UnitCategory.Volume        },
];

@Component({
  selector: 'app-create-item-dialog',
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
  templateUrl: './create-item-dialog.html',
  styleUrl: './create-item-dialog.css'
})
export class CreateItemDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CreateItemDialog>);
  private readonly itemService = inject(ItemService);
  private readonly apiErrorService = inject(ApiErrorService);

  readonly unitCategoryOptions: UnitCategoryOption[] = UNIT_CATEGORY_OPTIONS;

  readonly form = this.fb.nonNullable.group({
    name: [''],
    unitCategory: [UnitCategory.Uncategorized],
  });

  readonly loading = signal(false);
  readonly errorMessage = signal('');

  onSubmit(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.itemService.createItem(this.form.getRawValue()).subscribe({
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
