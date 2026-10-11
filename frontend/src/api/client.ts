const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080';

export function getToken(): string | null {
  return localStorage.getItem('leads_token');
}

export function setToken(token: string) {
  localStorage.setItem('leads_token', token);
}

export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers);
  if (!headers.has('Content-Type') && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }
  const token = getToken();
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE}${path}`, { ...init, headers });
  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || response.statusText);
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

export async function issueDevToken(userId: string, admin = false) {
  const data = await apiFetch<{ accessToken: string }>('/api/v1/auth/token', {
    method: 'POST',
    body: JSON.stringify({ userId, admin }),
  });
  setToken(data.accessToken);
}
