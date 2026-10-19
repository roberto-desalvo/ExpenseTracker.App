import { apiConfig } from "../config/api";

/**
 * Login demo: il BE emette un token di breve durata per l'utente di test (nessuna credenziale richiesta).
 * Il token vive in sessionStorage e viene rinnovato in automatico poco prima della scadenza.
 */
const STORAGE_KEY = "expense-tracker:demo-session";
const RENEW_MARGIN_MS = 60_000;

interface DemoSession {
  accessToken: string;
  expiresOn: string;
  userName: string;
}

interface DemoLoginResponse {
  accessToken: string;
  expiresOn: string;
  userName: string;
}

const readSession = (): DemoSession | null => {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as DemoSession) : null;
  } catch {
    return null;
  }
};

const writeSession = (session: DemoSession) => {
  try {
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Storage non disponibile: la sessione demo dura finché la pagina resta aperta.
  }
};

let memorySession: DemoSession | null = readSession();
let pendingLogin: Promise<DemoSession> | null = null;

const requestDemoSession = async (): Promise<DemoSession> => {
  let response: Response;
  try {
    response = await fetch(apiConfig.auth.demo, { method: "POST" });
  } catch {
    throw new Error("Impossibile contattare il server per avviare la demo.");
  }

  if (!response.ok) {
    throw new Error(
      response.status === 404
        ? "La demo non è al momento disponibile."
        : `Avvio della demo non riuscito (HTTP ${response.status}).`
    );
  }

  const body = (await response.json()) as DemoLoginResponse;
  const session: DemoSession = {
    accessToken: body.accessToken,
    expiresOn: body.expiresOn,
    userName: body.userName,
  };
  memorySession = session;
  writeSession(session);
  return session;
};

/** Richiede un nuovo token demo; chiamate concorrenti condividono la stessa richiesta. */
const loginDemo = (): Promise<DemoSession> => {
  pendingLogin ??= requestDemoSession().finally(() => {
    pendingLogin = null;
  });
  return pendingLogin;
};

export const hasDemoSession = (): boolean => memorySession !== null;

export const getDemoUserName = (): string => memorySession?.userName ?? "Demo";

export const startDemoSession = async (): Promise<void> => {
  await loginDemo();
};

export const clearDemoSession = () => {
  memorySession = null;
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // ignorato
  }
};

/** Token demo valido, rinnovato se in scadenza. Null se non c'è una sessione demo attiva. */
export const getDemoAccessToken = async (): Promise<string | null> => {
  const session = memorySession;
  if (!session) return null;

  const expiresAt = Date.parse(session.expiresOn);
  if (Number.isFinite(expiresAt) && expiresAt - Date.now() > RENEW_MARGIN_MS) {
    return session.accessToken;
  }

  const renewed = await loginDemo();
  return renewed.accessToken;
};
