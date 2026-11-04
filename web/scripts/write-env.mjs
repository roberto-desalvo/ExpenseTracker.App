// Genera src/environments/env.generated.ts con le variabili VITE_* lette da .env, .env.local
// e dall'ambiente di processo (che ha la precedenza, es. i secret della CI).
// Uso: node scripts/write-env.mjs <development|production>
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const mode = process.argv[2] === "development" ? "development" : "production";
const KEYS = [
  "VITE_EXPENSE_TRACKER_API_BASE_URL",
  "VITE_MSAL_CLIENT_ID",
  "VITE_MSAL_TENANT_ID",
  "VITE_MSAL_API_SCOPE",
  "VITE_DEV_AUTH_BYPASS",
  "VITE_DEMO_LOGIN_ENABLED",
];

const parseDotEnv = (path) => {
  if (!existsSync(path)) return {};
  const result = {};
  for (const line of readFileSync(path, "utf8").split(/\r?\n/)) {
    const match = /^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)\s*$/.exec(line);
    if (!match) continue;
    let value = match[2];
    if (/^(["']).*\1$/.test(value)) value = value.slice(1, -1);
    result[match[1]] = value;
  }
  return result;
};

const files = [".env", ".env.local", `.env.${mode}`, `.env.${mode}.local`];
const merged = Object.assign({}, ...files.map((f) => parseDotEnv(resolve(root, f))));
const values = Object.fromEntries(
  KEYS.map((key) => [key, process.env[key] ?? merged[key]]).filter(([, v]) => v !== undefined),
);

const out = resolve(root, "src/environments/env.generated.ts");
mkdirSync(dirname(out), { recursive: true });
writeFileSync(
  out,
  `// File generato da scripts/write-env.mjs: non modificare né committare.\n` +
    `export const rawEnv = ${JSON.stringify({ DEV: mode === "development", ...values }, null, 2)} as const;\n`,
);
console.log(`[write-env] ${mode}: ${Object.keys(values).join(", ") || "nessuna variabile"}`);
