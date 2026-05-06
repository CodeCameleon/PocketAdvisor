import { Component, inject } from '@angular/core';
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

  get isLoggedIn(): boolean {
    return this.sessionService.isLoggedIn();
  }

  logout(): void {
    this.sessionService.logout();
    this.router.navigate(['/auth/login']);
  }
}
