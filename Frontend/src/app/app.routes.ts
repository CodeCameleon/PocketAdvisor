import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth';
import { noAuthGuard } from './core/guards/no-auth';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'auth/login',
    pathMatch: 'full'
  },
  {
    path: 'accounts',
    canActivate: [authGuard],
    loadChildren: () => import('./accounts/accounts-module').then(m => m.AccountsModule)
  },
  {
    path: 'auth',
    canActivate: [noAuthGuard],
    loadChildren: () => import('./auth/auth-module').then(m => m.AuthModule)
  },
  {
    path: 'categories',
    canActivate: [authGuard],
    loadChildren: () => import('./categories/categories-module').then(m => m.CategoriesModule)
  },
  {
    path: '**',
    redirectTo: 'auth/login'
  }
];
