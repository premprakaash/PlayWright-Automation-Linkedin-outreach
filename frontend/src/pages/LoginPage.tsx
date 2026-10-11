import { useState } from 'react';
import { issueDevToken } from '../api/client';

export default function LoginPage() {
  const [userId, setUserId] = useState<string>(() => crypto.randomUUID());
  const [error, setError] = useState<string | null>(null);

  async function signIn() {
    try {
      await issueDevToken(userId);
      window.location.reload();
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Login failed');
    }
  }

  return (
    <div className="content" style={{ maxWidth: 480, margin: '4rem auto' }}>
      <div className="card">
        <h1>CodeInspector Leads</h1>
        <p>Sign in with your tenant user id (JWT dev token).</p>
        <label>Tenant User Id</label>
        <input value={userId} onChange={(e) => setUserId(e.target.value)} />
        {error && <p className="error">{error}</p>}
        <button style={{ marginTop: '1rem' }} onClick={signIn}>Continue</button>
      </div>
    </div>
  );
}
