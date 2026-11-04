/**
 * Login fittizio per lo sviluppo locale: l'utente indica solo un nome utente,
 * inviato al BE come `Authorization: DevUser <username>` (accettato solo con DevAuth abilitato).
 */
const STORAGE_KEY = "expense-tracker:dev-user";

export const DEV_USER_MAX_LENGTH = 60;

export const getDevUser = (): string | null => {
  try {
    return sessionStorage.getItem(STORAGE_KEY);
  } catch {
    return null;
  }
};

export const setDevUser = (userName: string) => {
  sessionStorage.setItem(STORAGE_KEY, userName);
};

export const clearDevUser = () => {
  sessionStorage.removeItem(STORAGE_KEY);
};

export const devAuthorizationHeader = (userName: string) => `DevUser ${userName}`;
