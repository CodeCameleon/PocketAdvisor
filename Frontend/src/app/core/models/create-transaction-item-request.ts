import { Unit } from '../enums/unit';

export interface CreateTransactionItemRequest {
  itemId: string;
  totalPrice: number;
  amount: number;
  unit: Unit;
}
