import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { AccountResponse } from '../models/account-response';
import { CreateAccountRequest } from '../models/create-account-request';
import { UpdateAccountNameRequest } from '../models/update-account-name-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/accounts`;

  /** Creates a new account for the authenticated user. */
  createAccount(request: CreateAccountRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  /** Returns all accounts belonging to the authenticated user. */
  getAccounts(): Observable<AccountResponse[]> {
    return this.http.get<AccountResponse[]>(`${this.base}`);
  }

  /** Updates the name of the specified account. */
  updateAccountName(id: string, request: UpdateAccountNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/name`, request);
  }

  /** Deletes the specified account. */
  deleteAccount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
