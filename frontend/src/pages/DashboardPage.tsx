import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../api/client';

type Summary = {
  totalCompanies: number;
  totalLeads: number;
  qualifiedLeads: number;
  draftsAwaitingReview: number;
  messagesSent: number;
  replies: number;
  positiveReplies: number;
  demosBooked: number;
  conversions: number;
  failedJobs: number;
};

export default function DashboardPage() {
  const { data, isLoading, error } = useQuery({
    queryKey: ['dashboard-summary'],
    queryFn: () => apiFetch<Summary>('/api/v1/dashboard/summary'),
  });

  if (isLoading) return <p>Loading dashboard…</p>;
  if (error) return <p className="error">{(error as Error).message}</p>;

  return (
    <div>
      <h1>Dashboard</h1>
      <div className="grid">
        <div className="card"><div>Companies</div><div className="metric">{data?.totalCompanies ?? 0}</div></div>
        <div className="card"><div>Leads</div><div className="metric">{data?.totalLeads ?? 0}</div></div>
        <div className="card"><div>Qualified</div><div className="metric">{data?.qualifiedLeads ?? 0}</div></div>
        <div className="card"><div>Drafts awaiting review</div><div className="metric">{data?.draftsAwaitingReview ?? 0}</div></div>
        <div className="card"><div>Sent manually</div><div className="metric">{data?.messagesSent ?? 0}</div></div>
        <div className="card"><div>Replies</div><div className="metric">{data?.replies ?? 0}</div></div>
        <div className="card"><div>Demos</div><div className="metric">{data?.demosBooked ?? 0}</div></div>
        <div className="card"><div>Conversions</div><div className="metric">{data?.conversions ?? 0}</div></div>
        <div className="card"><div>Failed jobs</div><div className="metric">{data?.failedJobs ?? 0}</div></div>
      </div>
    </div>
  );
}
