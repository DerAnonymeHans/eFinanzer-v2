// A single page app behind Keycloak: nothing is rendered or prerendered on a server, the static build only contains
// the fallback index.html (see vite.config.ts)
export const ssr = false;
export const prerender = false;
