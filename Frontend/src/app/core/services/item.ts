import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ItemResponse } from '../models/item-response';
import { CreateItemRequest } from '../models/create-item-request';
import { UpdateItemNameRequest } from '../models/update-item-name-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ItemService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/items`;

  /** Creates a new item for the authenticated user. */
  createItem(request: CreateItemRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  /** Returns all items belonging to the authenticated user. */
  getItems(): Observable<ItemResponse[]> {
    return this.http.get<ItemResponse[]>(`${this.base}`);
  }

  /** Updates the name of the specified item. */
  updateItemName(id: string, request: UpdateItemNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/name`, request);
  }

  /** Deletes the specified item. */
  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
