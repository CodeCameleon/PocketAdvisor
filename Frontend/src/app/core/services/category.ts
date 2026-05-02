import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CategoryResponse } from '../models/category-response';
import { CreateCategoryRequest } from '../models/create-category-request';
import { UpdateCategoryNameRequest } from '../models/update-category-name-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/categories`;

  /** Creates a new global category. Requires the Administrator role. */
  createGlobalCategory(request: CreateCategoryRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/global`, request);
  }

  /** Creates a new personal category for the authenticated user. */
  createPersonalCategory(request: CreateCategoryRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/personal`, request);
  }

  /** Returns all categories visible to the authenticated user. */
  getCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.base}`);
  }

  /** Updates the name of the specified global category. Requires the Administrator role. */
  updateGlobalCategoryName(id: string, request: UpdateCategoryNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/global/${id}/name`, request);
  }

  /** Updates the name of the specified personal category. */
  updatePersonalCategoryName(id: string, request: UpdateCategoryNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/personal/${id}/name`, request);
  }

  /** Deletes the specified global category. Requires the Administrator role. */
  deleteGlobalCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/global/${id}`);
  }

  /** Deletes the specified personal category. */
  deletePersonalCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/personal/${id}`);
  }
}
