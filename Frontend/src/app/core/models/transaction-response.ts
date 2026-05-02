import { TransactionItemResponse } from './transaction-item-response';

export interface TransactionResponse {
  id: string;
  occurredAt: string;
  categoryId: string;
  fromAccountId: string | null;
  toAccountId: string | null;
  items: TransactionItemResponse[];
}
