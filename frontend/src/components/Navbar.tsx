import { useAuthStore } from '../store/useAuthStore';
import { useCartStore } from '../store/useCartStore';
import { useGuestCartStore } from '../store/useGuestCartStore';
import { Link } from 'react-router-dom';

export const Navbar = () => {
  const { user, logout, isAuthenticated } = useAuthStore();
  const items = useCartStore((state) => state.items);
  const guestCount = useGuestCartStore((state) => state.totalItems());
  const cartCount = isAuthenticated ? items.length : guestCount;

  return (
    <nav className="sticky top-0 z-50 bg-slate-950/80 backdrop-blur-md border-b border-white/10">
      <div className="container mx-auto px-4 py-4 flex justify-between items-center">
        <Link to="/" className="flex items-center gap-2.5">
          <img src="/brand/logo-tight.png" alt="TaskFlow" className="h-7 w-auto object-contain opacity-80" />
          <span className="text-2xl font-bold bg-gradient-to-r from-purple-400 to-fuchsia-400 bg-clip-text text-transparent">
            TaskFlow
          </span>
        </Link>
        <div className="flex items-center gap-6">
          <Link to="/shop" className="text-slate-300 hover:text-purple-400 transition">
            Shop
          </Link>
          <Link to="/cart" className="text-slate-300 hover:text-purple-400 transition relative">
            Cart
            {cartCount > 0 && (
              <span className="absolute -top-2 -right-4 bg-purple-600 text-white text-[10px] font-bold rounded-full px-1.5 py-0.5">
                {cartCount}
              </span>
            )}
          </Link>
          {isAuthenticated ? (
            <>
              <Link to="/orders" className="text-slate-300 hover:text-purple-400 transition">
                Orders
              </Link>
              <span className="text-slate-400">Hello, {user?.firstName}</span>
              <button
                onClick={logout}
                className="text-purple-400 hover:text-purple-300 font-semibold transition"
              >
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-purple-400 hover:text-purple-300 font-semibold">
                Login
              </Link>
              <Link
                to="/register"
                className="bg-purple-600 text-white px-4 py-2 rounded-lg hover:bg-purple-700"
              >
                Register
              </Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
};
