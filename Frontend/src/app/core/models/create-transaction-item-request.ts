import { Unit } from '../enums/unit';

export interface CreateTransactionItemRequest {
  itemId: string | null;
  totalPrice: number;
  amount: number;
  unit: Unit;
}
