export interface Session { authenticated: boolean; username: string | null; roles: string[]; csrfToken: string }
let current: Session | undefined;
export async function loadSession(): Promise<Session> {
  const response = await fetch('/bff/session', { credentials: 'same-origin', cache: 'no-store' });
  if (!response.ok) throw new Error('Unable to check your session. Check that the backend is running.');
  current = await response.json() as Session;
  return current;
}
export async function csrfToken() { return (current ?? await loadSession()).csrfToken; }
export function expireSession() { current = undefined; window.dispatchEvent(new Event('cleanarq-session-expired')); }
export function signIn() { window.location.assign('/bff/login'); }
export async function signOut() {
  // A navigation POST lets the browser follow the OIDC logout redirects.
  const token = await csrfToken();
  const form = document.createElement('form'); form.method = 'POST'; form.action = '/bff/logout';
  const input = document.createElement('input'); input.type = 'hidden'; input.name = '__RequestVerificationToken'; input.value = token;
  form.appendChild(input); document.body.appendChild(form); form.submit();
}
