import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { AccountList } from './account-list/account-list';
import { AccountTransactions } from './account-transactions/account-transactions';

const routes: Routes = [
  { path: '', component: AccountList },
  { path: ':id/transactions', component: AccountTransactions }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class AccountsRoutingModule {}
