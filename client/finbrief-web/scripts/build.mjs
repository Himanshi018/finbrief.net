// `npm run build` runs this script. It runs `ng build` and, if the API_BASE_URL environment variable is set
// (Vercel sets it from your project settings), bakes that URL into the app with Angular's --define option.
//
//   API_BASE_URL=https://finbrief-api.onrender.com npm run build
import { spawnSync } from 'node:child_process';
import { createRequire } from 'node:module';

const apiBaseUrl = process.env.API_BASE_URL?.trim().replace(/\/+$/, ''); // no trailing "/"
const args = ['build'];

if (apiBaseUrl) {
  if (!/^https?:\/\//.test(apiBaseUrl)) {
    console.error(`API_BASE_URL must start with http:// or https:// (got "${apiBaseUrl}")`);
    process.exit(1);
  }
  // --define takes a JavaScript expression, so the URL is passed as a quoted string.
  args.push('--define', `API_BASE_URL=${JSON.stringify(apiBaseUrl)}`);
  console.log(`Building with API_BASE_URL=${apiBaseUrl}`);
} else {
  console.log('API_BASE_URL is not set; building with the local API URL (http://localhost:5132)');
}

// Run the Angular CLI with the same Node that runs this script (works on Windows, Mac, Linux and Vercel).
const ngCli = createRequire(import.meta.url).resolve('@angular/cli/bin/ng.js');
const result = spawnSync(process.execPath, [ngCli, ...args], { stdio: 'inherit' });
process.exit(result.status ?? 1);
