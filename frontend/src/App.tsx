import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BrowserRouter, NavLink, Route, Routes } from 'react-router-dom';
import DashboardPage from './pages/DashboardPage';
import ResearchPage from './pages/ResearchPage';
import LeadsPage from './pages/LeadsPage';
import LeadDetailPage from './pages/LeadDetailPage';
import ImportsPage from './pages/ImportsPage';
import SettingsPage from './pages/SettingsPage';
import LoginPage from './pages/LoginPage';
import './App.css';

const queryClient = new QueryClient();

function Shell() {
  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="brand">CodeInspector Leads</div>
        <nav>
          <NavLink to="/">Dashboard</NavLink>
          <NavLink to="/research">Research</NavLink>
          <NavLink to="/leads">Leads</NavLink>
          <NavLink to="/imports">Imports</NavLink>
          <NavLink to="/settings">Settings</NavLink>
        </nav>
      </aside>
      <main className="content">
        <Routes>
          <Route path="/" element={<DashboardPage />} />
          <Route path="/research" element={<ResearchPage />} />
          <Route path="/leads" element={<LeadsPage />} />
          <Route path="/leads/:id" element={<LeadDetailPage />} />
          <Route path="/imports" element={<ImportsPage />} />
          <Route path="/settings" element={<SettingsPage />} />
        </Routes>
      </main>
    </div>
  );
}

export default function App() {
  const token = localStorage.getItem('leads_token');
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        {token ? <Shell /> : <LoginPage />}
      </BrowserRouter>
    </QueryClientProvider>
  );
}
