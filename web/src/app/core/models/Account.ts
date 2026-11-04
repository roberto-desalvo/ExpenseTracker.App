interface Account{
    id: number;
    name: string;
    // usato dagli import per riconoscere i trasferimenti verso/da questo conto
    iban?: string | null;
}

export default Account;