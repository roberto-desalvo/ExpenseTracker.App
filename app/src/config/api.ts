import { appEnv } from "./env";

const apiRoot = `${appEnv.apiBaseUrl}/api`;

const resource = (segment: string) => `${apiRoot}/${segment}`;

const accountBase = resource("account");
const transactionBase = resource("transaction");
const categoryBase = resource("category");
const transferBase = resource("transfer");

export const apiConfig = {
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
    byId: (id: number) => `${transactionBase}/${id}`,
  },
  categories: {
    base: categoryBase,
    query: `${categoryBase}/query`,
    byId: (id: number) => `${categoryBase}/${id}`,
  },
  transfers: {
    base: transferBase,
  },
} as const;
