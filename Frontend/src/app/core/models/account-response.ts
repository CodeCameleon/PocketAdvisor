import { CurrencyCode } from '../enums/currency-code';

export interface AccountResponse {
  id: string;
  name: string;
  calculatedBalance: number;
  currencyCode: CurrencyCode;
}
