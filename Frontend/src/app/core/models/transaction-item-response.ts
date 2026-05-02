import { Unit } from '../enums/unit';

export interface TransactionItemResponse {
  itemId: string;
  totalPrice: number;
  amountValue: number;
  amountUnit: Unit;
}
