import { CurrencyCode } from '../enums/currency-code';

export interface CreateAccountRequest {
  name: string;
  balance: number;
  currencyCode: CurrencyCode;
}
