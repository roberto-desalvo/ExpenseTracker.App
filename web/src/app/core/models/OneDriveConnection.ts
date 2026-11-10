export type OneDriveConnectionStatus = "NotConnected" | "Pending" | "Connected" | "ReconnectRequired";

/** Stato del collegamento del OneDrive personale dell'utente per l'import automatico. */
export interface OneDriveConnection {
  /** false se l'import da OneDrive non è configurato in questo ambiente */
  available: boolean;
  status: OneDriveConnectionStatus;
  /** cartella nella radice del drive e sottocartelle delle banche in cui salvare gli estratti conto */
  folderName: string;
  bankFolders: string[];
  /** accesso in corso: codice da inserire sulla pagina Microsoft */
  userCode?: string | null;
  verificationUri?: string | null;
  codeExpiresOn?: string | null;
  pollIntervalSeconds?: number | null;
  driveOwner?: string | null;
  connectedOn?: string | null;
  lastSyncOn?: string | null;
  /** true se i file vengono importati da soli poco dopo il caricamento (notifiche attive) */
  automaticSync: boolean;
  lastError?: string | null;
}
