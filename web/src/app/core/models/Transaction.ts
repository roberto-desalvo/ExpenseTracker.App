class Transaction {
  public id: number;
  public date: Date;
  public description: string;
  public amount: number;
  public categoryId: number;
  public category: string;
  public accountId: number;
  public account: string; 
  /** Trasferimento a cui il movimento è collegato (sola lettura, dal BE). */
  public transferId?: number | null;
  /** true se il trasferimento collegato non ha il ramo di segno opposto. */
  public transferIncomplete?: boolean;
  /** true se il movimento è nella categoria "Money transfers" ma non è collegato a un trasferimento. */
  public transferMissing?: boolean;

  constructor(
    id: number,
    date: Date,
    description: string,
    amount: number,
    categoryId: number,
    category: string,
    accountId: number,
    account: string
  ) {
    this.id = id;
    this.date = date;
    this.description = description;
    this.amount = amount;
    this.categoryId = categoryId
    this.category = category;
    this.accountId = accountId;
    this.account = account;
  }
}

export default Transaction
