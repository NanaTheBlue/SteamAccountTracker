export class ApiError extends Error {
  status: number;
  
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

const API_URL = import.meta.env.VITE_API_URL || '';

async function fetchWithAuth(path: string, options: RequestInit = {}) {
  const url = `${API_URL}${path}`;
  const response = await fetch(url, {
    ...options,
    headers: {
      ...options.headers,
      'Content-Type': 'application/json',
    },
    credentials: 'include',
  });

  if (response.status === 401) {
    localStorage.removeItem('user');
    window.location.href = '/login';
    return;
  }

  if (!response.ok) {
    let errorMessage = 'An error occurred';
    try {
      const data = await response.json();
      errorMessage = data.message || data.error || errorMessage;
    } catch {
      const text = await response.text();
      errorMessage = text || errorMessage;
    }
    throw new ApiError(errorMessage, response.status);
  }

  return response.json();
}

export async function apiGet<T>(path: string): Promise<T> {
  return fetchWithAuth(path);
}

export async function apiPost<T>(path: string, body?: unknown): Promise<T> {
  return fetchWithAuth(path, {
    method: 'POST',
    body: body ? JSON.stringify(body) : undefined,
  });
}

export async function apiPut<T>(path: string, body?: unknown): Promise<T> {
  return fetchWithAuth(path, {
    method: 'PUT',
    body: body ? JSON.stringify(body) : undefined,
  });
}

export async function apiDelete<T>(path: string): Promise<T> {
  return fetchWithAuth(path, {
    method: 'DELETE',
  });
}
