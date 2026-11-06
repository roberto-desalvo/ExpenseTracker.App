import { appEnv } from "./env";

const apiRoot = `${appEnv.apiBaseUrl}/api`;

const resource = (segment: string) => `${apiRoot}/${segment}`;

const accountBase = resource("account");
const transactionBase = resource("transaction");
const categoryBase = resource("category");
const transferBase = resource("transfer");
const importBase = resource("imports");
const legacyMatchBase = resource("legacy-match");

export const apiConfig = {
  auth: {
    demo: resource("auth/demo"),
  },
  accounts: {
    base: accountBase,
    query: `${accountBase}/query`,
  },
  transactions: {
    base: transactionBase,
    query: `${transactionBase}/query`,
    monthOptions: `${transactionBase}/month-options`,
    landing: `${transactionBase}/landing`,
    series: `${transactionBase}/series`,
    stock: `${transactionBase}/stock`,
    bulkDelete: `${transactionBase}/bulk-delete`,
    bulkCategory: `${transactionBase}/bulk-category`,
    byId: (id: number) => `${transactionBase}/${id}`,
  },
  categories: {
    base: categoryBase,
    query: `${categoryBase}/query`,
    sync: `${categoryBase}/sync`,
    byId: (id: number) => `${categoryBase}/${id}`,
  },
  transfers: {
    base: transferBase,
  },
  accountTypes: {
    base: resource("account-type"),
  },
  imports: {
    base: importBase,
    query: `${importBase}/query`,
  },
  userSettings: {
    me: resource("user/me"),
    analyticsLayout: resource("user/analytics-layout"),
    appearance: resource("user/appearance"),
  },
  legacyMatch: {
    sheets: `${legacyMatchBase}/sheets`,
    month: `${legacyMatchBase}/month`,
    apply: `${legacyMatchBase}/apply`,
    create: `${legacyMatchBase}/create`,
    reset: `${legacyMatchBase}/reset`,
  },
} as const;
