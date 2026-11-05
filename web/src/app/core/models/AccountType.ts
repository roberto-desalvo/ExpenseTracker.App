/** Tipo di conto (banca o strumento), comune a tutti gli utenti. */
export interface AccountType {
  id: number;
  name: string;
  /** true se gli estratti conto di questo tipo si possono importare da file */
  supportsImport: boolean;
}
