import { Navigate } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';

export function AdminRoute({ children }: { children: React.ReactNode }) {
  const user = useAuthStore((state) => state.user);

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  // Check if user has Admin role (this is a simple check - in production, validate on backend)
  const isAdmin = user.role === 'Admin';

  if (!isAdmin) {
    return <Navigate to="/" replace />;
  }

  return <>{children}</>;
}
