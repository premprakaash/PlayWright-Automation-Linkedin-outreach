import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { useState } from 'react';
import { apiFetch } from '../api/client';

type Lead = {
  id: string;
  fullName: string;
  jobTitle?: string;
  linkedInProfileUrl?: string;
  fitScore: number;
  status: string;
  company?: { name: string };
};

export default function LeadsPage() {
  const [search, setSearch] = useState('');
  const qc = useQueryClient();
  const [form, setForm] = useState({
    companyName: '',
    companyWebsite: '',
    fullName: '',
    jobTitle: '',
    linkedInProfileUrl: '',
    businessEmail: '',
    technologySignals: '.NET;SaaS',
  });

  const leads = useQuery({
    queryKey: ['leads', search],
    queryFn: () => apiFetch<Lead[]>(`/api/v1/leads${search ? `?search=${encodeURIComponent(search)}` : ''}`),
  });

  const createCompanyAndLead = useMutation({
    mutationFn: async () => {
      const company = await apiFetch<{ id: string }>('/api/v1/companies', {
        method: 'POST',
        body: JSON.stringify({
          name: form.companyName,
          website: form.companyWebsite || null,
          technologySignals: form.technologySignals.split(';').map((s) => s.trim()).filter(Boolean),
          industry: 'SaaS',
          employeeRange: '11-50',
        }),
      });
      return apiFetch('/api/v1/leads', {
        method: 'POST',
        body: JSON.stringify({
          companyId: company.id,
          fullName: form.fullName,
          jobTitle: form.jobTitle,
          linkedInProfileUrl: form.linkedInProfileUrl,
          businessEmail: form.businessEmail,
        }),
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['leads'] }),
  });

  return (
    <div>
      <h1>Lead Management</h1>
      <div className="card">
        <input placeholder="Search leads" value={search} onChange={(e) => setSearch(e.target.value)} />
      </div>

      <div className="card">
        <h2>Manual lead entry</h2>
        <div className="grid">
          <input placeholder="Company name" value={form.companyName} onChange={(e) => setForm({ ...form, companyName: e.target.value })} />
          <input placeholder="Company website" value={form.companyWebsite} onChange={(e) => setForm({ ...form, companyWebsite: e.target.value })} />
          <input placeholder="Full name" value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          <input placeholder="Job title" value={form.jobTitle} onChange={(e) => setForm({ ...form, jobTitle: e.target.value })} />
          <input placeholder="LinkedIn profile URL" value={form.linkedInProfileUrl} onChange={(e) => setForm({ ...form, linkedInProfileUrl: e.target.value })} />
          <input placeholder="Business email" value={form.businessEmail} onChange={(e) => setForm({ ...form, businessEmail: e.target.value })} />
        </div>
        <button style={{ marginTop: '0.75rem' }} onClick={() => createCompanyAndLead.mutate()}>Save lead</button>
      </div>

      <div className="card">
        <table>
          <thead><tr><th>Name</th><th>Company</th><th>Score</th><th>Status</th><th></th></tr></thead>
          <tbody>
            {(leads.data ?? []).map((l) => (
              <tr key={l.id}>
                <td>{l.fullName}<div>{l.jobTitle}</div></td>
                <td>{l.company?.name}</td>
                <td>{l.fitScore}</td>
                <td>{l.status}</td>
                <td><Link to={`/leads/${l.id}`}>Details</Link></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
