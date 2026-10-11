import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { apiFetch } from '../api/client';

const presets = [
  'CTO .NET SaaS',
  'Engineering Manager C#',
  'Founder software development company',
  'Head of Engineering SaaS',
  'Technical Director .NET consultancy',
];

type Session = {
  id: string;
  keywords: string;
  searchUrl: string;
  status: string;
  openedAt: string;
};

export default function ResearchPage() {
  const [keywords, setKeywords] = useState(presets[0]);
  const qc = useQueryClient();

  const sessions = useQuery({
    queryKey: ['research-sessions'],
    queryFn: () => apiFetch<Session[]>('/api/v1/research-sessions'),
  });

  const createSession = useMutation({
    mutationFn: () => apiFetch<Session>('/api/v1/research-sessions', {
      method: 'POST',
      body: JSON.stringify({ keywords }),
    }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['research-sessions'] }),
  });

  return (
    <div>
      <h1>LinkedIn Research Assistant</h1>
      <div className="card">
        <label>Search keywords</label>
        <input value={keywords} onChange={(e) => setKeywords(e.target.value)} />
        <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', margin: '0.75rem 0' }}>
          {presets.map((p) => (
            <button key={p} className="secondary" type="button" onClick={() => setKeywords(p)}>{p}</button>
          ))}
        </div>
        <button onClick={() => createSession.mutate()} disabled={createSession.isPending}>Create research session</button>
        {createSession.data && (
          <p className="success" style={{ marginTop: '0.75rem' }}>
            Session created.{' '}
            <a href={createSession.data.searchUrl} target="_blank" rel="noreferrer noopener">
              Open LinkedIn Search
            </a>
          </p>
        )}
        <p style={{ marginTop: '0.75rem', opacity: 0.8 }}>
          Review LinkedIn results manually, then add selected prospects on the Leads page or import CSV.
        </p>
      </div>

      <div className="card">
        <h2>Session history</h2>
        <table>
          <thead><tr><th>Keywords</th><th>Status</th><th>Link</th><th>Opened</th></tr></thead>
          <tbody>
            {(sessions.data ?? []).map((s) => (
              <tr key={s.id}>
                <td>{s.keywords}</td>
                <td>{s.status}</td>
                <td><a href={s.searchUrl} target="_blank" rel="noreferrer noopener">Open</a></td>
                <td>{new Date(s.openedAt).toLocaleString()}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
