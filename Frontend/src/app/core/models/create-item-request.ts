import { UnitCategory } from '../enums/unit-category';

export interface CreateItemRequest {
  name: string;
  unitCategory: UnitCategory;
}
