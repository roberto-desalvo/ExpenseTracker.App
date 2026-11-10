import { inject } from "@angular/core";
import { CanMatchFn, Router } from "@angular/router";
import { ROUTES } from "../config/routes";
import { CurrentUserStore } from "../state/current-user.store";

/** Rotte non disponibili all'utente demo (import da file): torna a Movimenti (il BE lo verifica comunque). */
export const nonDemoGuard: CanMatchFn = async () => {
  const currentUser = inject(CurrentUserStore);
  const router = inject(Router);
  await currentUser.ensureLoaded();
  return !currentUser.isDemo() || router.parseUrl(ROUTES.transactions);
};
