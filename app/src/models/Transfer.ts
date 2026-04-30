export interface TransferPayload {
  id?: number;
  fromAccountId: number;
  toAccountId: number;
  amount: number;
  description: string;
  date?: Date | null;
}
