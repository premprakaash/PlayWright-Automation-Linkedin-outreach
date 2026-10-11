import { useState } from 'react';
import { apiFetch, getToken } from '../api/client';

export default function ImportsPage() {
  const [batchId, setBatchId] = useState<string | null>(null);
  const [result, setResult] = useState<unknown>(null);
  const [error, setError] = useState<string | null>(null);

  async function upload(file: File) {
    setError(null);
    const form = new FormData();
    form.append('file', file);
    const response = await fetch(`${import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080'}/api/v1/imports/csv`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: form,
    });
    if (!response.ok) {
      setError(await response.text());
      return;
    }
    const data = await response.json();
    setBatchId(data.id);
    setResult(data);
  }

  async function loadBatch() {
    if (!batchId) return;
    setResult(await apiFetch(`/api/v1/imports/${batchId}`));
  }

  return (
    <div>
      <h1>Import History</h1>
      <div className="card">
        <input type="file" accept=".csv,text/csv" onChange={(e) => e.target.files?.[0] && upload(e.target.files[0])} />
        {error && <p className="error">{error}</p>}
        {!!batchId && <button style={{ marginTop: '0.5rem' }} onClick={loadBatch}>Refresh batch</button>}
        {!!result && <pre style={{ whiteSpace: 'pre-wrap' }}>{JSON.stringify(result, null, 2)}</pre>}
      </div>
      <div className="card">
        <p>CSV columns: company_name, company_website, country, industry, employee_range, technology_signals, full_name, job_title, linkedin_profile_url, business_email, source_type, source_url, notes</p>
      </div>
    </div>
  );
}
