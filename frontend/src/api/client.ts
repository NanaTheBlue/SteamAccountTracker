export class ApiError extends Error {
  status: number;
  
  constructor(message: string, status: number) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

const API_URL = import.meta.env.VITE_API_URL || '';

async function send(path: string, options: RequestInit): Promise<Response> {
  const doFetch = () =>
    fetch(`${API_URL}${path}`, {
      ...options,
      headers: {
        ...options.headers,
        'Content-Type': 'application/json',
      },
      credentials: 'include',
    });

  // Local dev only (stripped from production builds): serve requests from the
  // in-browser mock when mock mode is on or the backend isn't reachable.
  if (import.meta.env.DEV) {
    const mock = await import('../dev/mockApi');
    if (mock.isMockMode()) return mock.mockFetch(path, options);
    try {
      const response = await doFetch();
      if (![502, 503, 504].includes(response.status)) return response;
    } catch {
      // Network error: backend not running
    }
    console.warn('[mock api] Backend unreachable, switching to mock API. Log out to leave mock mode.');
    mock.setMockMode(true);
    return mock.mockFetch(path, options);
  }

  return doFetch();
}

/** Pulls a human-readable message out of the error shapes the backend returns. */
function extractErrorMessage(body: string): string | null {
  if (!body) return null;
  try {
    const data = JSON.parse(body);
    if (typeof data === 'string') return data;
    if (data?.message) return data.message;
    if (data?.error) return data.error;
    // ASP.NET [ApiController] validation failure: { title, errors: { Field: [msg] } }
    if (data?.errors && typeof data.errors === 'object') {
      const first = Object.values(data.errors as Record<string, string[]>).flat()[0];
      if (first) return first;
    }
    if (data?.title) return data.title;
    return null;
  } catch {
    // Plain-text body, e.g. BadRequest("...")
    return body;
  }
}

async function fetchWithAuth(path: string, options: RequestInit = {}) {
  const response = await send(path, options);

  if (response.status === 401) {
    localStorage.removeItem('user');
    if (window.location.pathname !== '/login') {
      window.location.href = '/login';
    }
    throw new ApiError('Your session has expired. Please sign in again.', 401);
  }

  // Read the body exactly once; it can't be consumed twice.
  const body = await response.text();

  if (!response.ok) {
    throw new ApiError(extractErrorMessage(body) || 'An error occurred', response.status);
  }

  if (!body) return undefined;
  try {
    return JSON.parse(body);
  } catch {
    return body;
  }
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
