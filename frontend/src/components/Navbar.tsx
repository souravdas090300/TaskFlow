import { useAuthStore } from '../store/useAuthStore';
import { useCartStore } from '../store/useCartStore';
import { Link } from 'react-router-dom';

export const Navbar = () => {
  const { user, logout, isAuthenticated } = useAuthStore();
  const { items } = useCartStore((state: any) => state.items);

  return (
    <nav className="bg-white shadow-md">
      <div className="container mx-auto px-4 py-4 flex justify-between items-center">
        <Link to="/" className="text-2xl font-bold text-purple-600">
          TaskFlow
        </Link>
        <div className="flex items-center gap-6">
          <Link to="/shop" className="text-gray-700 hover:text-purple-600">
            Shop
          </Link>
          <Link to="/cart" className="text-gray-700 hover:text-purple-600">
            Cart ({items.length})
          </Link>
          {isAuthenticated ? (
            <>
              <Link to="/orders" className="text-gray-700 hover:text-purple-600">
                Orders
              </Link>
              <span className="text-gray-700">Hello, {user?.firstName}</span>
              <button
                onClick={logout}
                className="text-purple-600 hover:text-purple-800 font-semibold"
              >
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="text-purple-600 hover:text-purple-800 font-semibold">
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
