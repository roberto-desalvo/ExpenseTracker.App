/// <reference types="vite/client" />

interface ImportMetaEnv {
	readonly VITE_EXPENSE_TRACKER_API_BASE_URL: string;
	readonly VITE_MSAL_CLIENT_ID: string;
	readonly VITE_MSAL_TENANT_ID: string;
	readonly VITE_MSAL_API_SCOPE: string;
	readonly VITE_DEV_AUTH_BYPASS?: string;
	readonly VITE_DEMO_LOGIN_ENABLED?: string;
}

interface ImportMeta {
	readonly env: ImportMetaEnv;
}