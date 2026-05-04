import { Component, input } from '@angular/core';

@Component({
  selector: 'app-auth-card',
  imports: [],
  templateUrl: './auth-card.html'
})
export class AuthCard {
  readonly subtitle = input.required<string>();
}
