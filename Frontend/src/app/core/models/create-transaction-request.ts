import { CreateTransactionItemRequest } from './create-transaction-item-request';

export interface CreateTransactionRequest {
  occurredAt: string;
  categoryId: string;
  fromAccountId: string | null;
  toAccountId: string | null;
  items: CreateTransactionItemRequest[];
}
