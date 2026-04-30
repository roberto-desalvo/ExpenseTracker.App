import { PagedResult } from "./PagedResult";
import Transaction from "./Transaction";

export interface TransactionQueryResult extends PagedResult<Transaction> {
  totalIncomes: number;
  totalOutcomes: number;
  totalNet: number;
}
