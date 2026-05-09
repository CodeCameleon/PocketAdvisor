import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { GlobalCategoryList } from './global-category-list/global-category-list';

const routes: Routes = [
  { path: 'categories', component: GlobalCategoryList }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AdminRoutingModule {}
