import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ItemService } from '../../core/services/item';
import { ItemResponse } from '../../core/models/item-response';
import { UnitCategory } from '../../core/enums/unit-category';
import { CreateItemDialog } from '../create-item-dialog/create-item-dialog';
import { DeleteItemDialog } from '../delete-item-dialog/delete-item-dialog';
import { UpdateItemNameDialog } from '../update-item-name-dialog/update-item-name-dialog';

@Component({
  selector: 'app-item-list',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './item-list.html',
  styleUrl: './item-list.css'
})
export class ItemList implements OnInit {
  private readonly itemService = inject(ItemService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  readonly items = signal<ItemResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadItems();
  }

  openDetail(item: ItemResponse): void {
    this.router.navigate(['/items', item.id]);
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateItemDialog, {
      width: '520px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.loadItems();
      }
    });
  }

  openRenameDialog(item: ItemResponse): void {
    const ref = this.dialog.open(UpdateItemNameDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { id: item.id, name: item.name },
    });

    ref.afterClosed().subscribe((updated: boolean) => {
      if (updated) {
        this.loadItems();
      }
    });
  }

  openDeleteDialog(item: ItemResponse): void {
    const ref = this.dialog.open(DeleteItemDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { id: item.id, name: item.name },
    });

    ref.afterClosed().subscribe((deleted: boolean) => {
      if (deleted) {
        this.loadItems();
      }
    });
  }

  /** Returns a human-readable label for a UnitCategory enum value. */
  unitCategoryLabel(category: UnitCategory): string {
    return UnitCategory[category] ?? 'Unknown';
  }

  /** Returns a Material icon name appropriate for a given UnitCategory. */
  unitCategoryIcon(category: UnitCategory): string {
    switch (category) {
      case UnitCategory.Length:    return 'straighten';
      case UnitCategory.Mass:      return 'scale';
      case UnitCategory.Area:      return 'crop_free';
      case UnitCategory.Volume:    return 'water_drop';
      case UnitCategory.Time:      return 'schedule';
      case UnitCategory.Energy:    return 'bolt';
      case UnitCategory.DataSize:  return 'storage';
      default:                     return 'category';
    }
  }

  private loadItems(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.itemService.getItems().subscribe({
      next: (items) => {
        this.items.set(items);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load items. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
