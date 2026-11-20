import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '../store/useAuthStore';

const navLinkClass = ({ isActive }: { isActive: boolean }) =>
  `px-3 py-2 rounded-lg text-sm font-medium transition ${
    isActive ? 'bg-purple-600/20 text-purple-300' : 'text-slate-400 hover:text-white hover:bg-white/5'
  }`;

export const AdminLayout = () => {
  const { user, logout } = useAuthStore();
  const navigate = useNavigate();

  const handleLogout = () => {
    void logout();
    void navigate('/admin/login');
  };

  return (
    <div className="min-h-screen bg-slate-100">
      <nav className="bg-slate-950 border-b border-white/10 sticky top-0 z-50">
        <div className="container mx-auto px-4 py-3 flex justify-between items-center">
          <div className="flex items-center gap-8">
            <Link to="/admin" className="flex items-center gap-2">
              <img src="/brand/logo-tight.png" alt="TaskFlow" className="h-5 w-auto object-contain opacity-80" />
              <span className="bg-gradient-to-r from-purple-400 to-fuchsia-400 bg-clip-text text-transparent text-xl font-bold">
                TaskFlow
              </span>
              <span className="text-[10px] font-bold uppercase tracking-widest bg-purple-600/20 text-purple-300 border border-purple-500/30 px-2 py-0.5 rounded">
                Admin
              </span>
            </Link>
            <div className="hidden md:flex items-center gap-1">
              <NavLink to="/admin" end className={navLinkClass}>
                Dashboard
              </NavLink>
              <NavLink to="/admin/orders" className={navLinkClass}>
                Orders
              </NavLink>
              <NavLink to="/admin/products" className={navLinkClass}>
                Products
              </NavLink>
              <NavLink to="/admin/categories" className={navLinkClass}>
                Categories
              </NavLink>
              <NavLink to="/admin/payment-issues" className={navLinkClass}>
                Payment Issues
              </NavLink>
            </div>
          </div>
          <div className="flex items-center gap-4">
            <span className="text-sm text-slate-400 hidden sm:inline">{user?.email}</span>
            <Link to="/" className="text-sm text-slate-400 hover:text-white transition">
              ← Store
            </Link>
            <button
              onClick={handleLogout}
              className="text-sm text-purple-400 hover:text-purple-300 font-semibold transition"
            >
              Logout
            </button>
          </div>
        </div>
        {/* Mobile nav */}
        <div className="md:hidden flex gap-1 px-4 pb-3">
          <NavLink to="/admin" end className={navLinkClass}>
            Dashboard
          </NavLink>
          <NavLink to="/admin/orders" className={navLinkClass}>
            Orders
          </NavLink>
          <NavLink to="/admin/products" className={navLinkClass}>
            Products
          </NavLink>
          <NavLink to="/admin/categories" className={navLinkClass}>
            Categories
          </NavLink>
          <NavLink to="/admin/payment-issues" className={navLinkClass}>
            Payment Issues
          </NavLink>
        </div>
      </nav>
      <main className="container mx-auto px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
};

export default AdminLayout;
