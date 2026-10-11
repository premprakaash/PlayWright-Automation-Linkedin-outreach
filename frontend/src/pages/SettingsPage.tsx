export default function SettingsPage() {
  return (
    <div>
      <h1>Settings</h1>
      <div className="card">
        <h2>Implemented configuration</h2>
        <ul>
          <li>Daily processing schedule: 9:00 AM Asia/Kolkata (worker cron)</li>
          <li>Batch size: 100 companies per run</li>
          <li>Scoring rules: v1 heuristic rules in API/worker</li>
          <li>Import limits: 5 MB / 10,000 rows (configurable in API appsettings)</li>
          <li>Browser worker: headless Chromium with domain allowlist support</li>
          <li>Message templates: deterministic template v1</li>
        </ul>
      </div>
    </div>
  );
}
