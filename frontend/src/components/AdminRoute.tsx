import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';

export function AdminRoute({ children }: { children: React.ReactNode }) {
  const { user, isAuthenticated } = useAuthStore();

  if (!isAuthenticated || !user) {
    return <Navigate to="/admin/login" replace />;
  }

  // Client-side UX gate only — every /api/admin/* endpoint enforces the Admin role on the backend.
  if (user.role !== 'Admin') {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
