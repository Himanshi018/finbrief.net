// Vercel serverless function that runs Angular's server-side rendering.
//
// Vercel serves files that exist in dist/finbrief-web/browser (JavaScript, CSS, the prerendered landing
// page) directly from its CDN. Every other request (/feed, /articles/12, ...) is sent here by the rewrite
// in vercel.json, and Angular renders the page. reqHandler is exported by src/server.ts.
export default async function handler(req, res) {
  const { reqHandler } = await import('../dist/finbrief-web/server/server.mjs');
  return reqHandler(req, res);
}
