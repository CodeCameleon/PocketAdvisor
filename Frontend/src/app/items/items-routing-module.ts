import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { ItemList } from './item-list/item-list';
import { ItemDetail } from './item-detail/item-detail';

const routes: Routes = [
  { path: '', component: ItemList },
  { path: ':id', component: ItemDetail }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class ItemsRoutingModule {}
