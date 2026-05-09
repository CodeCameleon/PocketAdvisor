import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { ItemList } from './item-list/item-list';

const routes: Routes = [
  { path: '', component: ItemList }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ItemsRoutingModule {}
