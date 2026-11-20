import { useState, type SubmitEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { categoriesApi } from '../api/products';
import { useAuthStore } from '../store/useAuthStore';
import { useCartStore } from '../store/useCartStore';
import { useGuestCartStore } from '../store/useGuestCartStore';
import { useThemeStore } from '../store/useThemeStore';

export const Navbar = () => {
  const navigate = useNavigate();
  const { user, logout, isAuthenticated } = useAuthStore();
  const items = useCartStore((state) => state.items);
  const guestCount = useGuestCartStore((state) => state.totalItems());
  const cartCount = isAuthenticated ? items.length : guestCount;
  const mode = useThemeStore((state) => state.mode);
  const toggleTheme = useThemeStore((state) => state.toggle);
  const [search, setSearch] = useState('');
  const [mobileOpen, setMobileOpen] = useState(false);
  const [accountOpen, setAccountOpen] = useState(false);
  const [categoriesOpen, setCategoriesOpen] = useState(false);

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  });

  const submitSearch = (event: SubmitEvent<HTMLFormElement>) => {
    event.preventDefault();
    const q = search.trim();
    void navigate(q ? `/shop?search=${encodeURIComponent(q)}` : '/shop');
  };

  return (
    <nav className="sticky top-0 z-50 bg-white/90 dark:bg-slate-950/85 backdrop-blur-md border-b border-gray-200 dark:border-white/10">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-between gap-4 py-3">
          <Link to="/" className="flex items-center gap-2 shrink-0">
            <span className="text-2xl font-bold text-purple-700 dark:text-transparent dark:bg-gradient-to-r dark:from-purple-400 dark:to-fuchsia-400 dark:bg-clip-text">
              Mercato
            </span>
          </Link>

          <form onSubmit={submitSearch} className="hidden md:flex flex-1 max-w-xl">
            <input
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              placeholder="Search products..."
              className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-white/15 bg-white dark:bg-slate-900 text-gray-900 dark:text-white placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-purple-500"
            />
          </form>

          <div className="hidden lg:flex items-center gap-1">
            <Link to="/shop" className="px-3 py-2 text-sm font-medium text-gray-700 dark:text-slate-300 hover:text-purple-700 dark:hover:text-purple-300 rounded-lg hover:bg-gray-100 dark:hover:bg-white/5">
              Shop
            </Link>
            <div className="relative" onMouseLeave={() => setCategoriesOpen(false)}>
              <button
                type="button"
                onMouseEnter={() => setCategoriesOpen(true)}
                onClick={() => setCategoriesOpen((open) => !open)}
                className="px-3 py-2 text-sm font-medium text-gray-700 dark:text-slate-300 hover:text-purple-700 dark:hover:text-purple-300 rounded-lg hover:bg-gray-100 dark:hover:bg-white/5"
              >
                Categories
              </button>
              {categoriesOpen && categories && categories.length > 0 && (
                <div className="absolute left-0 mt-1 w-56 rounded-xl border border-gray-200 dark:border-white/10 bg-white dark:bg-slate-900 shadow-xl py-2">
                  {categories.slice(0, 8).map((category) => (
                    <Link
                      key={category.id}
                      to={`/shop?categoryId=${category.id}`}
                      onClick={() => setCategoriesOpen(false)}
                      className="block px-4 py-2 text-sm text-gray-700 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5"
                    >
                      {category.name}
                    </Link>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="flex items-center gap-2">
            <button
              type="button"
              onClick={toggleTheme}
              className="p-2 rounded-lg text-gray-600 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5"
              aria-label="Toggle theme"
            >
              {mode === 'light' ? '🌙' : '☀️'}
            </button>
            <Link to="/cart" className="relative p-2 rounded-lg text-gray-600 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5" aria-label="Cart">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" d="M2.25 3h1.386c.51 0 .955.343 1.087.835l.383 1.437M7.5 14.25a3 3 0 0 0-3 3h15.75m-12.75-3h11.218c1.121-2.3 2.1-4.684 2.924-7.138a60.114 60.114 0 0 0-16.536-1.84M7.5 14.25 5.106 5.272M6 20.25a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Zm12.75 0a.75.75 0 1 1-1.5 0 .75.75 0 0 1 1.5 0Z" />
              </svg>
              {cartCount > 0 && (
                <span className="absolute -top-1 -right-1 bg-purple-600 text-white text-[10px] font-bold rounded-full px-1.5 py-0.5">
                  {cartCount}
                </span>
              )}
            </Link>

            {isAuthenticated ? (
              <div className="relative hidden sm:block">
                <button
                  type="button"
                  onClick={() => setAccountOpen((open) => !open)}
                  className="px-3 py-2 text-sm font-medium text-gray-700 dark:text-slate-300 hover:text-purple-700 dark:hover:text-purple-300 rounded-lg hover:bg-gray-100 dark:hover:bg-white/5"
                >
                  {user?.firstName || user?.email}
                </button>
                {accountOpen && (
                  <div className="absolute right-0 mt-1 w-44 rounded-xl border border-gray-200 dark:border-white/10 bg-white dark:bg-slate-900 shadow-xl py-2">
                    <Link to="/orders" onClick={() => setAccountOpen(false)} className="block px-4 py-2 text-sm text-gray-700 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5">Orders</Link>
                    <button type="button" onClick={logout} className="w-full text-left px-4 py-2 text-sm text-purple-700 dark:text-purple-300 hover:bg-gray-100 dark:hover:bg-white/5">Logout</button>
                  </div>
                )}
              </div>
            ) : (
              <div className="hidden sm:flex items-center gap-2">
                <Link to="/login" className="px-3 py-2 text-sm font-semibold text-purple-700 dark:text-purple-300 hover:bg-gray-100 dark:hover:bg-white/5 rounded-lg">Login</Link>
                <Link to="/register" className="bg-purple-600 text-white px-4 py-2 rounded-lg text-sm font-semibold hover:bg-purple-700">Register</Link>
              </div>
            )}

            <button type="button" onClick={() => setMobileOpen((open) => !open)} className="lg:hidden p-2 rounded-lg text-gray-600 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5" aria-label="Menu">
              <svg className="w-6 h-6" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24" aria-hidden="true">
                <path strokeLinecap="round" strokeLinejoin="round" d="M3.75 6.75h16.5M3.75 12h16.5m-16.5 5.25h16.5" />
              </svg>
            </button>
          </div>
        </div>

        <form onSubmit={submitSearch} className="md:hidden pb-3">
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search products..."
            className="w-full px-4 py-2 rounded-lg border border-gray-300 dark:border-white/15 bg-white dark:bg-slate-900 text-gray-900 dark:text-white"
          />
        </form>

        {mobileOpen && (
          <div className="lg:hidden pb-4 space-y-1">
            <Link to="/shop" onClick={() => setMobileOpen(false)} className="block px-3 py-2 rounded-lg text-sm font-medium text-gray-700 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5">Shop</Link>
            {categories?.slice(0, 8).map((category) => (
              <Link key={category.id} to={`/shop?categoryId=${category.id}`} onClick={() => setMobileOpen(false)} className="block px-3 py-2 rounded-lg text-sm text-gray-600 dark:text-slate-400 hover:bg-gray-100 dark:hover:bg-white/5">
                {category.name}
              </Link>
            ))}
            {isAuthenticated ? (
              <>
                <Link to="/orders" onClick={() => setMobileOpen(false)} className="block px-3 py-2 rounded-lg text-sm font-medium text-gray-700 dark:text-slate-300 hover:bg-gray-100 dark:hover:bg-white/5">Orders</Link>
                <button type="button" onClick={logout} className="w-full text-left px-3 py-2 rounded-lg text-sm font-semibold text-purple-700 dark:text-purple-300">Logout</button>
              </>
            ) : (
              <>
                <Link to="/login" onClick={() => setMobileOpen(false)} className="block px-3 py-2 rounded-lg text-sm font-semibold text-purple-700 dark:text-purple-300">Login</Link>
                <Link to="/register" onClick={() => setMobileOpen(false)} className="block px-3 py-2 rounded-lg text-sm font-semibold bg-purple-600 text-white text-center">Register</Link>
              </>
            )}
          </div>
        )}
      </div>
    </nav>
  );
};
