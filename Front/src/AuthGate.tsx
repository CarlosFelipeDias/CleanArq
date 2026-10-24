import { useEffect, useState } from 'react';
import { initializeAuth, keycloak, signIn, signOut } from './auth';
import App from './App';
import Login from './components/Login';
export default function AuthGate() {
  const [ready, setReady] = useState(false);
  const [authenticated, setAuthenticated] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    const expired = () => { setAuthenticated(false); setError('Your session has expired. Please sign in again.'); };
    window.addEventListener('cleanarq-session-expired', expired);
    initializeAuth().then(value => { if (active) { setAuthenticated(value); setReady(true); } })
      .catch(() => { if (active) {setReady(true); setError('Unable to connect to the identity service. Check that Keycloak is running, then reload this page.');} });
    return () => {active = false; window.removeEventListener('cleanarq-session-expired', expired);};
  }, []);
  if (!ready) return <main className="empty" role="status">Checking your session…</main>;
  if (!authenticated) return <Login error={error} enter={() => { void signIn().catch(() => setError('Unable to start sign-in. Please reload and try again.')); }} />;
  const roles = keycloak.realmAccess?.roles || [];
  if (!roles.some(role => role === 'admin' || role === 'user')) return <main className="empty"><h1>Access denied</h1><p>Your account needs the admin or user role to access this workspace.</p><button className="secondary" onClick={() => void signOut()}>Sign out</button></main>;
  return <App username={keycloak.tokenParsed?.preferred_username || 'Account'} roles={roles} logout={() => void signOut()} />;
}
