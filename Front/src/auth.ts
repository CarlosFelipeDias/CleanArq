import Keycloak from 'keycloak-js';

export const keycloak = new Keycloak({
  url: import.meta.env.VITE_KEYCLOAK_URL || 'http://localhost:8180',
  realm: import.meta.env.VITE_KEYCLOAK_REALM || 'CleanArq',
  clientId: import.meta.env.VITE_KEYCLOAK_CLIENT_ID || 'cleanarq-frontend',
});
// init must run once, including under React StrictMode.
let initialization: Promise<boolean> | undefined;
export function initializeAuth() {
  return initialization ??= keycloak.init({
    onLoad: 'check-sso', pkceMethod: 'S256', checkLoginIframe: false,
    redirectUri: window.location.origin + '/', responseMode: 'query',
  });
}
export const signIn = () => keycloak.login({ redirectUri: window.location.origin + '/' });
export const signOut = () => keycloak.logout({ redirectUri: window.location.origin + '/' });
export async function accessToken() {
  if (!keycloak.authenticated) throw new Error('Your session has expired. Please sign in again.');
  try {
    await keycloak.updateToken(30);
    if (!keycloak.token) throw new Error('Missing access token.');
    return keycloak.token;
  } catch {
    keycloak.clearToken();
    window.dispatchEvent(new Event('cleanarq-session-expired'));
    throw new Error('Your session has expired. Please sign in again.');
  }
}
