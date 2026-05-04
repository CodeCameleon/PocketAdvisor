import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AccountList } from './account-list/account-list';

const routes: Routes = [
  { path: '', component: AccountList }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccountsRoutingModule {}
