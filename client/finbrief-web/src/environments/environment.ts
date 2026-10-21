// Used by production builds (`ng build`).
//
// The API URL is not written here. It comes from the API_BASE_URL environment variable at build time
// (set it in Vercel → Settings → Environment Variables). scripts/build.mjs passes it to Angular with
// `--define`, which replaces API_BASE_URL in the code with the real value.
// Without it (e.g. a local `npm run build`), the local API is used.
declare const API_BASE_URL: string | undefined;

export const environment = {
  apiBaseUrl: (typeof API_BASE_URL === 'string' && API_BASE_URL) || 'http://localhost:5132',
};
