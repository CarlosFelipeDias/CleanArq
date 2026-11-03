import { useRef, useState } from 'react';
import type { FormEvent } from 'react';
import axios from 'axios';
import { api, errorMessage } from '../api';

interface CreatedUser { id: string; email: string; firstName: string; lastName: string; role: string }
interface RegistrationError { message: string; reviewRequired: boolean; userId?: string; accountState?: string; traceId?: string }

export default function UserRegistration({ onBusyChange }: { onBusyChange: (busy: boolean) => void }) {
  const [email, setEmail] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [password, setPassword] = useState('');
  const [confirmation, setConfirmation] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<RegistrationError>();
  const [created, setCreated] = useState<CreatedUser>();
  const submitting = useRef(false);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (submitting.current || error?.reviewRequired || created) return;
    setError(undefined);
    if (!firstName.trim() || !lastName.trim() || /\s/.test(email.trim())) {
      setError({ message: 'Provide a valid email, first name and last name.', reviewRequired: false });
      return;
    }
    if (password !== confirmation) {
      setError({ message: 'The temporary passwords do not match.', reviewRequired: false });
      return;
    }
    submitting.current = true;
    setBusy(true);
    onBusyChange(true);
    try {
      const response = await api.post<CreatedUser>('/users', {
        email: email.trim(), firstName: firstName.trim(), lastName: lastName.trim(), temporaryPassword: password,
      });
      setCreated(response.data);
    } catch (failure) {
      const response = axios.isAxiosError(failure) ? failure.response : undefined;
      const problem = response?.data;
      // A lost response may follow a successful write. Never retry provisioning automatically.
      const reviewRequired = !response || response.status >= 500 || problem?.code === 'Incomplete' || problem?.code === 'OutcomeUnknown';
      setError({
        message: typeof problem?.detail === 'string' ? problem.detail : reviewRequired
          ? 'Account creation could not be confirmed. Check Keycloak before submitting another request.' : errorMessage(failure),
        reviewRequired,
        userId: typeof problem?.userId === 'string' ? problem.userId : undefined,
        accountState: typeof problem?.accountState === 'string' ? problem.accountState : undefined,
        traceId: typeof problem?.traceId === 'string' ? problem.traceId : undefined,
      });
    } finally {
      setPassword(''); setConfirmation('');
      submitting.current = false; setBusy(false);
      onBusyChange(false);
    }
  };

  const reset = () => {
    setEmail(''); setFirstName(''); setLastName(''); setPassword(''); setConfirmation('');
    setError(undefined); setCreated(undefined);
  };

  return <>
    <p className="eyebrow">ACCESS MANAGEMENT</p>
    <div className="page-heading"><div><h1>Users<span className="brand-dot">.</span></h1><p className="muted">Create an account for a new workspace user.</p></div></div>
    <section className="registration-panel">
      <h2>New user</h2>
      <p className="muted registration-intro">Accounts are created in Keycloak with the <strong>user</strong> role. Administrator accounts are managed directly in Keycloak.</p>
      {created ? <div>
        <div role="status" className="success"><strong>User created successfully.</strong><p>{created.firstName} {created.lastName} ({created.email}) can now sign in. They must change their temporary password at first sign-in.</p></div>
        <p className="registration-intro">Share the temporary password through a private channel. It is no longer displayed or retained by this form.</p>
        <button className="primary" onClick={reset}>Create another user</button>
      </div> : <form onSubmit={event => void submit(event)}>
        <fieldset disabled={busy || error?.reviewRequired}>
          <div className="form-grid">
            <label htmlFor="user-first-name">First name<input id="user-first-name" name="firstName" autoComplete="off" required maxLength={100} value={firstName} onChange={event => setFirstName(event.target.value)} /></label>
            <label htmlFor="user-last-name">Last name<input id="user-last-name" name="lastName" autoComplete="off" required maxLength={100} value={lastName} onChange={event => setLastName(event.target.value)} /></label>
          </div>
          <label htmlFor="user-email">Email<input id="user-email" name="email" type="email" autoComplete="off" required maxLength={254} value={email} onChange={event => setEmail(event.target.value)} /><small>The email address will also be the username.</small></label>
          <label htmlFor="user-password">Temporary password<input id="user-password" name="temporaryPassword" type="password" autoComplete="new-password" required minLength={8} maxLength={128} value={password} onChange={event => setPassword(event.target.value)} aria-describedby="password-help" /><small id="password-help">8–128 characters. Keycloak may require additional complexity. The user must change it at first sign-in.</small></label>
          <label htmlFor="user-password-confirm">Confirm temporary password<input id="user-password-confirm" name="confirmPassword" type="password" autoComplete="new-password" required minLength={8} maxLength={128} value={confirmation} onChange={event => setConfirmation(event.target.value)} /></label>
        </fieldset>
        {error && <div role="alert" className={error.reviewRequired ? 'notice' : 'error'}>
          <p>{error.message}</p>
          {error.reviewRequired && <p>Review this email in Keycloak before trying again. The account may already exist or may need its role and activation checked.</p>}
          {error.userId && <p className="registration-reference">Account ID: {error.userId}</p>}
          {error.accountState && <p>Account state: {error.accountState}</p>}
          {error.traceId && <p className="registration-reference">Reference: {error.traceId}</p>}
        </div>}
        <div className="modal-actions">
          {error?.reviewRequired ? <button type="button" className="secondary" onClick={reset}>I checked Keycloak — start a new request</button>
            : <button type="submit" className="primary" disabled={busy}>{busy ? 'Creating user…' : 'Create user'}</button>}
        </div>
        {busy && <p role="status" className="registration-intro">Creating the account and assigning its user role. Please wait before leaving this page.</p>}
      </form>}
    </section>
  </>;
}
