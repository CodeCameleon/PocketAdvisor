import { Routes } from '@angular/router';

import { adminGuard } from './core/guards/admin';
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
    path: 'admin',
    canActivate: [adminGuard],
    loadChildren: () => import('./admin/admin-module').then(m => m.AdminModule)
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
    path: 'items',
    canActivate: [authGuard],
    loadChildren: () => import('./items/items-module').then(m => m.ItemsModule)
  },
  {
    path: '**',
    redirectTo: 'auth/login'
  }
];
