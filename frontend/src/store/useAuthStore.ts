import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { authApi } from '../api/auth';
import type { User } from '../api/auth';

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, firstName: string, lastName: string) => Promise<void>;
  logout: () => void;
  loadUser: () => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      token: null,
      isAuthenticated: false,

      login: async (email, password) => {
        const data = await authApi.login({ email, password });
        set({ user: data.user, token: data.token, isAuthenticated: true });
      },

      register: async (email, password, firstName, lastName) => {
        const data = await authApi.register({ email, password, firstName, lastName });
        set({ user: data.user, token: data.token, isAuthenticated: true });
      },

      logout: () => {
        authApi.logout();
        set({ user: null, token: null, isAuthenticated: false });
      },

      loadUser: async () => {
        try {
          const user = await authApi.getCurrentUser();
          const token = authApi.getToken();
          set({ user, token: token || null, isAuthenticated: !!token });
        } catch (error) {
          // User not authenticated or token expired - clear state
          set({ user: null, token: null, isAuthenticated: false });
        }
      },
    }),
    {
      name: 'auth-storage',
    }
  )
);
