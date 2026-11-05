interface Account{
    id: number;
    name: string;
    // usato dagli import per riconoscere i trasferimenti verso/da questo conto
    iban?: string | null;
    // tipo di conto (banca): determina quale import scrive su questo conto; al massimo un conto per tipo
    accountTypeId?: number | null;
}

export default Account;
