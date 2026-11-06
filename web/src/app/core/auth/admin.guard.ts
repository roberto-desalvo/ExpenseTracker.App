import { inject } from "@angular/core";
import { CanMatchFn, Router } from "@angular/router";
import { ROUTES } from "../config/routes";
import { CurrentUserStore } from "../state/current-user.store";

/** Rotte riservate agli admin: gli altri utenti tornano a Movimenti (il BE verifica comunque il ruolo). */
export const adminGuard: CanMatchFn = async () => {
  const currentUser = inject(CurrentUserStore);
  const router = inject(Router);
  await currentUser.ensureLoaded();
  return currentUser.isAdmin() || router.parseUrl(ROUTES.transactions);
};
