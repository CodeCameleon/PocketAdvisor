import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { TransactionResponse } from '../models/transaction-response';
import { CreateTransactionRequest } from '../models/create-transaction-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/transactions`;

  /** Creates a new transaction with its items. */
  createTransaction(request: CreateTransactionRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  /** Returns all transactions for the specified account. */
  getTransactions(accountId: string): Observable<TransactionResponse[]> {
    return this.http.get<TransactionResponse[]>(`${this.base}`, {
      params: { accountId },
    });
  }

  /** Deletes the specified transaction and all of its items. */
  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  /** Removes a single item from the specified transaction. */
  deleteTransactionItem(transactionId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${transactionId}/items/${itemId}`);
  }
}
