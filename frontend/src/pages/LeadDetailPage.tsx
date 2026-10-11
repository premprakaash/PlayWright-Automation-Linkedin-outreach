import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useParams } from 'react-router-dom';
import { useState } from 'react';
import { apiFetch } from '../api/client';

type Lead = {
  id: string;
  fullName: string;
  jobTitle?: string;
  linkedInProfileUrl?: string;
  fitScore: number;
  scoreReasons: Array<{ rule: string; points: number; explanation: string }>;
  company?: { name: string; website?: string };
};

type Draft = {
  id: string;
  body: string;
  status: string;
  evidenceUsed: Record<string, string>;
};

export default function LeadDetailPage() {
  const { id } = useParams();
  const qc = useQueryClient();
  const [draftBody, setDraftBody] = useState<string | null>(null);
  const [draftId, setDraftId] = useState<string | null>(null);

  const lead = useQuery({
    queryKey: ['lead', id],
    queryFn: () => apiFetch<Lead>(`/api/v1/leads/${id}`),
    enabled: !!id,
  });

  const activities = useQuery({
    queryKey: ['activities', id],
    queryFn: () => apiFetch<Array<{ activityType: string; occurredAt: string; notes?: string }>>(`/api/v1/leads/${id}/activities`),
    enabled: !!id,
  });

  const generateDraft = useMutation({
    mutationFn: () => apiFetch<Draft>(`/api/v1/leads/${id}/drafts`, {
      method: 'POST',
      body: JSON.stringify({ channel: 'LinkedIn' }),
    }),
    onSuccess: (draft) => {
      setDraftBody(draft.body);
      setDraftId(draft.id);
      qc.invalidateQueries({ queryKey: ['activities', id] });
    },
  });

  const approveDraft = useMutation({
    mutationFn: () => apiFetch(`/api/v1/drafts/${draftId}/approve`, { method: 'POST' }),
  });

  const recordSent = useMutation({
    mutationFn: () => apiFetch(`/api/v1/leads/${id}/activities`, {
      method: 'POST',
      body: JSON.stringify({ activityType: 'SentManually', draftId }),
    }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['activities', id] }),
  });

  if (lead.isLoading) return <p>Loading…</p>;
  if (lead.error || !lead.data) return <p className="error">Lead not found</p>;

  return (
    <div>
      <h1>{lead.data.fullName}</h1>
      <div className="card">
        <p><strong>Company:</strong> {lead.data.company?.name}</p>
        <p><strong>Role:</strong> {lead.data.jobTitle}</p>
        <p><strong>Score:</strong> {lead.data.fitScore}</p>
        {lead.data.linkedInProfileUrl && (
          <p><a href={lead.data.linkedInProfileUrl} target="_blank" rel="noreferrer noopener">Open LinkedIn profile</a></p>
        )}
        <ul>
          {lead.data.scoreReasons?.map((r) => (
            <li key={r.rule}>{r.explanation} (+{r.points})</li>
          ))}
        </ul>
      </div>

      <div className="card">
        <h2>Message draft</h2>
        <button onClick={() => generateDraft.mutate()}>Generate draft</button>
        {draftBody && (
          <>
            <textarea rows={10} value={draftBody} onChange={(e) => setDraftBody(e.target.value)} style={{ marginTop: '0.75rem' }} />
            <div style={{ display: 'flex', gap: '0.5rem', marginTop: '0.5rem' }}>
              <button className="secondary" onClick={() => draftBody && navigator.clipboard.writeText(draftBody)}>Copy to clipboard</button>
              <button onClick={() => approveDraft.mutate()}>Approve</button>
              <button className="secondary" onClick={() => recordSent.mutate()}>Confirm sent manually</button>
            </div>
          </>
        )}
      </div>

      <div className="card">
        <h2>Outreach timeline</h2>
        <ul>
          {(activities.data ?? []).map((a, idx) => (
            <li key={idx}>{a.activityType} — {new Date(a.occurredAt).toLocaleString()} {a.notes}</li>
          ))}
        </ul>
      </div>
    </div>
  );
}
