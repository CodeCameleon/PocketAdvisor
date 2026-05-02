# Angular Services — API Layer

**Date:** 2026-05-02

---

## Turn 1

### User

> I want you to create the angular service that call the backend endpoints.

### Assistant Thinking

- Read all six backend controllers: `AccountController`, `CategoryController`, `ItemController`, `SessionController`, `TransactionController`, `UserController`.
- Read all request/response models and the four backend enums (`ECurrencyCode`, `EUnit`, `EUnitCategory`, `EUserRole`).
- Read the existing Angular frontend structure — at this point only the root `app.ts`, `app.config.ts`, and `app.routes.ts` existed, with no services or models yet.
- Noted memory reference for Angular CLI usage: run `npx @angular/cli` from the Frontend project root.
- Planned to: generate enums, generate interface stubs via CLI, fill them in, generate a single `ApiService`, and wire up `HttpClient`.

### Assistant Response

Used the Angular CLI to scaffold all model and enum files, then wrote their content. Created a single monolithic `ApiService` covering all endpoints, and added `provideHttpClient(withFetch())` to `app.config.ts`.

---

#### New: `src/app/core/enums/currency-code.ts`

```typescript
export enum CurrencyCode {
  All = 8,
  Dzd = 12,
  Ars = 32,
  Aud = 36,
  Bsd = 44,
  Bhd = 48,
  Bdt = 50,
  Amd = 51,
  Bbd = 52,
  Bmd = 60,
  Btn = 64,
  Bob = 68,
  Bwp = 72,
  Bzd = 84,
  Sbd = 90,
  Bnd = 96,
  Mmk = 104,
  Bif = 108,
  Khr = 116,
  Cad = 124,
  Cve = 132,
  Kyd = 136,
  Lkr = 144,
  Clp = 152,
  Cny = 156,
  Cop = 170,
  Kmf = 174,
  Crc = 188,
  Cup = 192,
  Czk = 203,
  Dkk = 208,
  Dop = 214,
  Svc = 222,
  Etb = 230,
  Ern = 232,
  Fkp = 238,
  Fjd = 242,
  Djf = 262,
  Gmd = 270,
  Gip = 292,
  Gtq = 320,
  Gnf = 324,
  Gyd = 328,
  Htg = 332,
  Hnl = 340,
  Hkd = 344,
  Huf = 348,
  Isk = 352,
  Inr = 356,
  Idr = 360,
  Irr = 364,
  Iqd = 368,
  Ils = 376,
  Jmd = 388,
  Jpy = 392,
  Kzt = 398,
  Jod = 400,
  Kes = 404,
  Kpw = 408,
  Krw = 410,
  Kwd = 414,
  Kgs = 417,
  Lak = 418,
  Lbp = 422,
  Lsl = 426,
  Lrd = 430,
  Lyd = 434,
  Mop = 446,
  Mwk = 454,
  Myr = 458,
  Mvr = 462,
  Mur = 480,
  Mxn = 484,
  Mnt = 496,
  Mdl = 498,
  Mad = 504,
  Omr = 512,
  Nad = 516,
  Npr = 524,
  Xcg = 532,
  Awg = 533,
  Vuv = 548,
  Nzd = 554,
  Nio = 558,
  Ngn = 566,
  Nok = 578,
  Pkr = 586,
  Pab = 590,
  Pgk = 598,
  Pyg = 600,
  Pen = 604,
  Php = 608,
  Qar = 634,
  Rub = 643,
  Rwf = 646,
  Shp = 654,
  Sar = 682,
  Scr = 690,
  Sgd = 702,
  Vnd = 704,
  Sos = 706,
  Zar = 710,
  Szl = 748,
  Sek = 752,
  Chf = 756,
  Syp = 760,
  Thb = 764,
  Top = 776,
  Ttd = 780,
  Aed = 784,
  Tnd = 788,
  Ugx = 800,
  Mkd = 807,
  Egp = 818,
  Gbp = 826,
  Tzs = 834,
  Usd = 840,
  Uyu = 858,
  Uzs = 860,
  Wst = 882,
  Yer = 886,
  Twd = 901,
  Zwg = 924,
  Sle = 925,
  Ves = 928,
  Mru = 929,
  Stn = 930,
  Byn = 933,
  Tmt = 934,
  Ghs = 936,
  Sdg = 938,
  Rsd = 941,
  Mzn = 943,
  Azn = 944,
  Ron = 946,
  Try = 949,
  Xaf = 950,
  Xcd = 951,
  Xof = 952,
  Xpf = 953,
  Zmw = 967,
  Srd = 968,
  Mga = 969,
  Afn = 971,
  Tjs = 972,
  Aoa = 973,
  Cdf = 976,
  Bam = 977,
  Eur = 978,
  Uah = 980,
  Gel = 981,
  Pln = 985,
  Brl = 986,
}
```

---

#### New: `src/app/core/enums/unit-category.ts`

```typescript
export enum UnitCategory {
  Uncategorized = 1,
  Length = 2,
  Mass = 3,
  Area = 4,
  Volume = 5,
  Time = 6,
  Energy = 7,
  DataSize = 8,
}
```

---

#### New: `src/app/core/enums/unit.ts`

```typescript
export enum Unit {
  // Uncategorized
  Piece = 1,

  // Length
  Millimeter = 101,
  Centimeter = 102,
  Meter = 103,
  Kilometer = 104,

  // Mass
  Milligram = 201,
  Gram = 202,
  Kilogram = 203,
  Tonne = 204,

  // Area
  SquareMeter = 301,
  SquareKilometer = 302,
  Acre = 306,
  Hectare = 307,

  // Volume
  Milliliter = 401,
  Liter = 402,
  CubicMeter = 403,

  // Time
  Second = 501,
  Minute = 502,
  Hour = 503,
  Day = 504,
  Month = 505,
  Year = 506,

  // Energy
  Joule = 601,
  Kilojoule = 602,
  KilowattHour = 603,

  // DataSize
  Byte = 701,
  Kilobyte = 702,
  Megabyte = 703,
  Gigabyte = 704,
  Terabyte = 705,
}
```

---

#### New: `src/app/core/models/account-response.ts`

```typescript
import { CurrencyCode } from '../enums/currency-code';

export interface AccountResponse {
  id: string;
  name: string;
  calculatedBalance: number;
  currencyCode: CurrencyCode;
}
```

---

#### New: `src/app/core/models/category-response.ts`

```typescript
export interface CategoryResponse {
  id: string;
  name: string;
}
```

---

#### New: `src/app/core/models/item-response.ts`

```typescript
import { UnitCategory } from '../enums/unit-category';

export interface ItemResponse {
  id: string;
  name: string;
  unitCategory: UnitCategory;
}
```

---

#### New: `src/app/core/models/transaction-item-response.ts`

```typescript
import { Unit } from '../enums/unit';

export interface TransactionItemResponse {
  itemId: string;
  totalPrice: number;
  amountValue: number;
  amountUnit: Unit;
}
```

---

#### New: `src/app/core/models/transaction-response.ts`

```typescript
import { TransactionItemResponse } from './transaction-item-response';

export interface TransactionResponse {
  id: string;
  occurredAt: string;
  categoryId: string;
  fromAccountId: string | null;
  toAccountId: string | null;
  items: TransactionItemResponse[];
}
```

---

#### New: `src/app/core/models/login-response.ts`

```typescript
export interface LoginResponse {
  jsonWebToken: string;
  refreshToken: string;
}
```

---

#### New: `src/app/core/models/create-account-request.ts`

```typescript
import { CurrencyCode } from '../enums/currency-code';

export interface CreateAccountRequest {
  name: string;
  balance: number;
  currencyCode: CurrencyCode;
}
```

---

#### New: `src/app/core/models/update-account-name-request.ts`

```typescript
export interface UpdateAccountNameRequest {
  name: string;
}
```

---

#### New: `src/app/core/models/create-category-request.ts`

```typescript
export interface CreateCategoryRequest {
  name: string;
}
```

---

#### New: `src/app/core/models/update-category-name-request.ts`

```typescript
export interface UpdateCategoryNameRequest {
  name: string;
}
```

---

#### New: `src/app/core/models/create-item-request.ts`

```typescript
import { UnitCategory } from '../enums/unit-category';

export interface CreateItemRequest {
  name: string;
  unitCategory: UnitCategory;
}
```

---

#### New: `src/app/core/models/update-item-name-request.ts`

```typescript
export interface UpdateItemNameRequest {
  name: string;
}
```

---

#### New: `src/app/core/models/create-transaction-item-request.ts`

```typescript
import { Unit } from '../enums/unit';

export interface CreateTransactionItemRequest {
  itemId: string;
  totalPrice: number;
  amount: number;
  unit: Unit;
}
```

---

#### New: `src/app/core/models/create-transaction-request.ts`

```typescript
import { CreateTransactionItemRequest } from './create-transaction-item-request';

export interface CreateTransactionRequest {
  occurredAt: string;
  categoryId: string;
  fromAccountId: string | null;
  toAccountId: string | null;
  items: CreateTransactionItemRequest[];
}
```

---

#### New: `src/app/core/models/login-request.ts`

```typescript
export interface LoginRequest {
  email: string;
  password: string;
}
```

---

#### New: `src/app/core/models/refresh-request.ts`

```typescript
export interface RefreshRequest {
  refreshToken: string;
}
```

---

#### New: `src/app/core/models/create-user-request.ts`

```typescript
export interface CreateUserRequest {
  email: string;
  password: string;
  confirmPassword: string;
}
```

---

#### New: `src/app/core/models/forgot-password-request.ts`

```typescript
export interface ForgotPasswordRequest {
  email: string;
}
```

---

#### New: `src/app/core/models/reset-password-request.ts`

```typescript
export interface ResetPasswordRequest {
  token: string;
  password: string;
  confirmPassword: string;
}
```

---

#### New: `src/app/core/models/verify-email-request.ts`

```typescript
export interface VerifyEmailRequest {
  token: string;
}
```

---

#### Modified: `src/app/app.config.ts`

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch } from '@angular/common/http';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch()),
  ]
};
```

---

### Design Decisions

- GUIDs from the backend are typed as `string` in TypeScript — Angular's `HttpClient` deserialises JSON and there is no native UUID type.
- `occurredAt` on transactions is typed as `string` rather than `Date` because `HttpClient` does not auto-convert ISO date strings; the component layer can parse when needed.
- `fromAccountId` and `toAccountId` are `string | null` to match the backend nullability (income vs. expense semantics).
- `withFetch()` is passed to `provideHttpClient` so the app uses the Fetch API under SSR/hydration, which is required when `provideClientHydration` is also present.

---

## Turn 2

### User

> I want you to separate each controller's endpoints into its own service file. So it can be easier to use than one big api service.

### Assistant Thinking

- Scaffolded six services via `ng generate service` for each controller group.
- Deleted the monolithic `api.ts` and its spec using `allow_cowork_file_delete`.
- Each service gets its own `base` URL constant pointing to its controller route.
- Verified zero TypeScript errors after the split.

### Assistant Response

Deleted `api.ts` and replaced it with six focused services, all `providedIn: 'root'`.

---

#### New: `src/app/core/services/session.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { RefreshRequest } from '../models/refresh-request';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/sessions`;

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/login`, request);
  }

  refresh(request: RefreshRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/refresh`, request);
  }
}
```

---

#### New: `src/app/core/services/user.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateUserRequest } from '../models/create-user-request';
import { ForgotPasswordRequest } from '../models/forgot-password-request';
import { ResetPasswordRequest } from '../models/reset-password-request';
import { VerifyEmailRequest } from '../models/verify-email-request';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/users`;

  createUser(request: CreateUserRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  forgotPassword(request: ForgotPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/forgot-password`, request);
  }

  resetPassword(request: ResetPasswordRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/reset-password`, request);
  }

  verifyEmail(request: VerifyEmailRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/verify-email`, request);
  }
}
```

---

#### New: `src/app/core/services/account.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { AccountResponse } from '../models/account-response';
import { CreateAccountRequest } from '../models/create-account-request';
import { UpdateAccountNameRequest } from '../models/update-account-name-request';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class AccountService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/accounts`;

  getAccounts(): Observable<AccountResponse[]> {
    return this.http.get<AccountResponse[]>(`${this.base}`);
  }

  createAccount(request: CreateAccountRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  updateAccountName(id: string, request: UpdateAccountNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/name`, request);
  }

  deleteAccount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
```

---

#### New: `src/app/core/services/category.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CategoryResponse } from '../models/category-response';
import { CreateCategoryRequest } from '../models/create-category-request';
import { UpdateCategoryNameRequest } from '../models/update-category-name-request';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/categories`;

  getCategories(): Observable<CategoryResponse[]> {
    return this.http.get<CategoryResponse[]>(`${this.base}`);
  }

  createGlobalCategory(request: CreateCategoryRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/global`, request);
  }

  createPersonalCategory(request: CreateCategoryRequest): Observable<void> {
    return this.http.post<void>(`${this.base}/personal`, request);
  }

  updateGlobalCategoryName(id: string, request: UpdateCategoryNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/global/${id}/name`, request);
  }

  updatePersonalCategoryName(id: string, request: UpdateCategoryNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/personal/${id}/name`, request);
  }

  deleteGlobalCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/global/${id}`);
  }

  deletePersonalCategory(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/personal/${id}`);
  }
}
```

---

#### New: `src/app/core/services/item.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ItemResponse } from '../models/item-response';
import { CreateItemRequest } from '../models/create-item-request';
import { UpdateItemNameRequest } from '../models/update-item-name-request';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ItemService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/items`;

  getItems(): Observable<ItemResponse[]> {
    return this.http.get<ItemResponse[]>(`${this.base}`);
  }

  createItem(request: CreateItemRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  updateItemName(id: string, request: UpdateItemNameRequest): Observable<void> {
    return this.http.patch<void>(`${this.base}/${id}/name`, request);
  }

  deleteItem(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
```

---

#### New: `src/app/core/services/transaction.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { TransactionResponse } from '../models/transaction-response';
import { CreateTransactionRequest } from '../models/create-transaction-request';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class TransactionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/transactions`;

  getTransactions(accountId: string): Observable<TransactionResponse[]> {
    return this.http.get<TransactionResponse[]>(`${this.base}`, {
      params: { accountId },
    });
  }

  createTransaction(request: CreateTransactionRequest): Observable<void> {
    return this.http.post<void>(`${this.base}`, request);
  }

  deleteTransaction(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  deleteTransactionItem(transactionId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${transactionId}/items/${itemId}`);
  }
}
```

---

#### Deleted: `src/app/core/services/api.ts` and `api.spec.ts`

---

### Design Decisions

- One service per controller keeps injection simple — a component that only manages accounts injects only `AccountService`.
- All services remain `providedIn: 'root'` (singleton) so there is no need to register them in any module or component.

---

## Turn 3

### User

> I want you to update the session service to store the returned tokens in the local storage. Also create a logout function too that deletes the tokens.

### Assistant Thinking

- Updated `SessionService` to use `tap` on both `login` and `refresh` to persist tokens to `localStorage` immediately after a successful response.
- Added `logout()` to remove both keys.
- Added `getJwt()`, `getRefreshToken()`, and `isLoggedIn()` helpers — these are needed by guards and the upcoming interceptor.
- Exposed storage keys as `static readonly` constants so other classes (e.g. an interceptor) can reference them without duplicating string literals.

### Assistant Response

---

#### Modified: `src/app/core/services/session.ts`

```typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { LoginRequest } from '../models/login-request';
import { LoginResponse } from '../models/login-response';
import { RefreshRequest } from '../models/refresh-request';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class SessionService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/sessions`;

  static readonly JwtKey = 'jwt';
  static readonly RefreshTokenKey = 'refreshToken';

  /** Authenticates a user, stores the returned tokens, and returns the response. */
  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/login`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  /** Rotates the refresh token, stores the new tokens, and returns the response. */
  refresh(request: RefreshRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/refresh`, request).pipe(
      tap(response => this.storeTokens(response))
    );
  }

  /** Removes the stored tokens, ending the local session. */
  logout(): void {
    localStorage.removeItem(SessionService.JwtKey);
    localStorage.removeItem(SessionService.RefreshTokenKey);
  }

  /** Returns the stored JWT, or null if not present. */
  getJwt(): string | null {
    return localStorage.getItem(SessionService.JwtKey);
  }

  /** Returns the stored refresh token, or null if not present. */
  getRefreshToken(): string | null {
    return localStorage.getItem(SessionService.RefreshTokenKey);
  }

  /** Returns true if a JWT is currently stored. */
  isLoggedIn(): boolean {
    return this.getJwt() !== null;
  }

  private storeTokens(response: LoginResponse): void {
    localStorage.setItem(SessionService.JwtKey, response.jsonWebToken);
    localStorage.setItem(SessionService.RefreshTokenKey, response.refreshToken);
  }
}
```

---

### Design Decisions

- `tap` is used so the `Observable` passes the `LoginResponse` through unchanged — callers can still subscribe and act on the response, while storage is handled as a side effect.
- Storage keys are `static readonly` on the class so the interceptor (next turn) can reference `SessionService.JwtKey` without hardcoding the string.
- `isLoggedIn()` is a convenience helper for route guards — it checks for the presence of a token rather than validating it (expiry validation belongs server-side or in a dedicated guard).

---

## Turn 4

### User

> Now I want you to create a session interceptor that attaches the token.

### Assistant Thinking

- Generated `session-interceptor.ts` via `ng generate interceptor core/interceptors/session`.
- Angular 21 generates functional interceptors (`HttpInterceptorFn`) rather than class-based ones — used that style.
- The interceptor reads the JWT via `inject(SessionService).getJwt()` and clones the request with an `Authorization: Bearer` header only when a token is present.
- Registered via `withInterceptors([sessionInterceptor])` inside `provideHttpClient` in `app.config.ts`.

### Assistant Response

---

#### New: `src/app/core/interceptors/session-interceptor.ts`

```typescript
import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

import { SessionService } from '../services/session';

export const sessionInterceptor: HttpInterceptorFn = (req, next) => {
  const jwt = inject(SessionService).getJwt();

  if (!jwt) {
    return next(req);
  }

  return next(req.clone({
    setHeaders: { Authorization: `Bearer ${jwt}` },
  }));
};
```

---

#### Modified: `src/app/app.config.ts`

```typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { provideClientHydration, withEventReplay } from '@angular/platform-browser';
import { sessionInterceptor } from './core/interceptors/session-interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideClientHydration(withEventReplay()),
    provideHttpClient(withFetch(), withInterceptors([sessionInterceptor])),
  ]
};
```

---

### Design Decisions

- Unauthenticated requests (no token in storage) pass through untouched — the public endpoints (`/sessions/login`, `/users`, `/users/forgot-password`, etc.) do not need and should not receive an `Authorization` header.
- Using `req.clone` is required because `HttpRequest` objects are immutable.
- The functional interceptor style (`HttpInterceptorFn`) is idiomatic for Angular 17+ standalone apps and avoids the boilerplate of a class-based interceptor.

---

## Turn 5

### User

> Export

### Assistant Thinking

- Retrieved export format from memory (`reference_export_format.md`).
- Read final versions of all files from the filesystem to ensure accuracy.
- Reconstructed all turns from the conversation in chronological order.
- Saved to `Prompts\angular-services.md`.
