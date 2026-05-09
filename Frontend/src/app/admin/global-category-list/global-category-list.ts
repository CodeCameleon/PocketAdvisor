import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { CategoryService } from '../../core/services/category';
import { CategoryResponse } from '../../core/models/category-response';
import { CreateGlobalCategoryDialog } from '../create-global-category-dialog/create-global-category-dialog';
import { DeleteGlobalCategoryDialog } from '../delete-global-category-dialog/delete-global-category-dialog';
import { UpdateGlobalCategoryNameDialog } from '../update-global-category-name-dialog/update-global-category-name-dialog';

@Component({
  selector: 'app-global-category-list',
  imports: [
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './global-category-list.html',
  styleUrl: './global-category-list.css'
})
export class GlobalCategoryList implements OnInit {
  private readonly categoryService = inject(CategoryService);
  private readonly dialog = inject(MatDialog);

  readonly categories = signal<CategoryResponse[]>([]);
  readonly loading = signal(true);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    this.loadCategories();
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateGlobalCategoryDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
    });

    ref.afterClosed().subscribe((created: boolean) => {
      if (created) {
        this.loadCategories();
      }
    });
  }

  openRenameDialog(category: CategoryResponse): void {
    const ref = this.dialog.open(UpdateGlobalCategoryNameDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { id: category.id, name: category.name },
    });

    ref.afterClosed().subscribe((updated: boolean) => {
      if (updated) {
        this.loadCategories();
      }
    });
  }

  openDeleteDialog(category: CategoryResponse): void {
    const ref = this.dialog.open(DeleteGlobalCategoryDialog, {
      width: '480px',
      maxWidth: '95vw',
      autoFocus: 'first-tabbable',
      restoreFocus: true,
      data: { id: category.id, name: category.name },
    });

    ref.afterClosed().subscribe((deleted: boolean) => {
      if (deleted) {
        this.loadCategories();
      }
    });
  }

  private loadCategories(): void {
    this.loading.set(true);
    this.errorMessage.set('');

    this.categoryService.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories.filter(c => c.isGlobal));
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Failed to load categories. Please try again.');
        this.loading.set(false);
      },
    });
  }
}
