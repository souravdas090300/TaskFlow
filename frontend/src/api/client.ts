import axios from 'axios';

// Empty -> same-origin "/api" requests. In dev, vite proxies /api to localhost:5000;
// in docker, the frontend nginx proxies /api to the api container.
const API_BASE_URL = import.meta.env.VITE_API_URL || '';

export const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add JWT token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let refreshPromise: Promise<void> | null = null;

export async function refreshAuthTokens(): Promise<void> {
  const refreshToken = localStorage.getItem('refreshToken');
  if (!refreshToken) throw new Error('No refresh token');
  const response = await axios.post(`${API_BASE_URL}/api/auth/refresh`, { refreshToken });
  localStorage.setItem('token', response.data.token);
  localStorage.setItem('refreshToken', response.data.refreshToken);
  localStorage.setItem('user', JSON.stringify(response.data.user));
}

export function clearAuthStorage(): void {
  localStorage.removeItem('token');
  localStorage.removeItem('refreshToken');
  localStorage.removeItem('user');
}

export async function ensureFreshAccessToken(): Promise<void> {
  // Share one in-flight refresh across concurrent 401s
  refreshPromise ??= refreshAuthTokens().finally(() => { refreshPromise = null; });
  await refreshPromise;
}

function clearAuthAndRedirect() {
  clearAuthStorage();
  const path = window.location.pathname;
  if (path !== '/login' && path !== '/admin/login') {
    window.location.href = '/login';
  }
}

// Response interceptor: on 401, try one token refresh then retry the request once
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status !== 401 || originalRequest?._retried) {
      if (error.response?.status === 401) clearAuthAndRedirect();
      return Promise.reject(error);
    }

    originalRequest._retried = true;
    try {
      await ensureFreshAccessToken();
      originalRequest.headers.Authorization = `Bearer ${localStorage.getItem('token')}`;
      return api(originalRequest);
    } catch {
      clearAuthAndRedirect();
      return Promise.reject(error);
    }
  }
);

export default api;
