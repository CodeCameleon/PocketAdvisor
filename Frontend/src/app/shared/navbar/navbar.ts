import { Component, computed, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { SessionService } from '../../core/services/session';

@Component({
  selector: 'app-navbar',
  imports: [
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule
  ],
  templateUrl: './navbar.html',
  styleUrl: './navbar.css'
})
export class Navbar {
  private readonly sessionService = inject(SessionService);
  private readonly router = inject(Router);

  readonly isLoggedIn = this.sessionService.isLoggedIn;

  readonly isAdmin = computed(() => {
    const jwt = this.sessionService.getJwt();

    if (!jwt) {
      return false;
    }

    try {
      const payload = JSON.parse(atob(jwt.split('.')[1]));
      const role: unknown = payload['role'] ?? payload['roles'] ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

      if (Array.isArray(role)) {
        return role.includes('Administrator');
      }

      return role === 'Administrator';
    } catch {
      return false;
    }
  });

  logout(): void {
    this.sessionService.logout();
    this.router.navigate(['/auth/login']);
  }
}
