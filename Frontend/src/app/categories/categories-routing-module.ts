import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { CategoryList } from './category-list/category-list';

const routes: Routes = [
  { path: '', component: CategoryList }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class CategoriesRoutingModule {}
