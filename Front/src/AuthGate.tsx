import { useEffect, useState } from 'react';
import { loadSession, signIn, signOut } from './auth';
import type { Session } from './auth';
import App from './App';
import Login from './components/Login';
export default function AuthGate() {
  const [session, setSession] = useState<Session>();
  const [ready, setReady] = useState(false);
  const [error, setError] = useState(() => new URLSearchParams(window.location.search).has('authError') ? 'Sign-in could not be completed. Please try again.' : '');
  useEffect(() => {
    let active = true;
    if (new URLSearchParams(window.location.search).has('authError')) window.history.replaceState(null, '', '/');
    const expired = () => { setSession(undefined); setError('Your session has expired. Please sign in again.'); };
    window.addEventListener('cleanarq-session-expired', expired);
    loadSession().then(value => { if (active) { setSession(value); setReady(true); } })
      .catch(() => { if (active) {setReady(true); setError('Unable to connect to the session service. Check that the backend is running, then reload this page.');} });
    return () => {active = false; window.removeEventListener('cleanarq-session-expired', expired);};
  }, []);
  const logout = () => { void signOut().catch(() => setError('Unable to sign out. Please reload and try again.')); };
  if (!ready) return <main className="empty" role="status">Checking your session…</main>;
  if (!session?.authenticated) return <Login error={error} enter={signIn} />;
  if (!session.roles.some(role => role === 'admin' || role === 'user')) return <main className="empty"><h1>Access denied</h1><p>Your account needs the admin or user role to access this workspace.</p><button className="secondary" onClick={logout}>Sign out</button></main>;
  return <>{error && <p className="error" role="alert">{error}</p>}<App username={session.username || 'Account'} roles={session.roles} logout={logout} /></>;
}
