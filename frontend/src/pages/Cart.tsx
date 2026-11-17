import { useCartStore } from '../store/useCartStore';
import { useGuestCartStore } from '../store/useGuestCartStore';
import { useAuthStore } from '../store/useAuthStore';
import { Link } from 'react-router-dom';

export const Cart = () => {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const { items, total, updateItem, removeItem } = useCartStore();
  const guest = useGuestCartStore();

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="container mx-auto px-4">
          <h1 className="text-3xl font-bold mb-8">Shopping Cart</h1>
          {guest.items.length === 0 ? (
            <div className="text-center py-12">
              <p className="text-gray-600 mb-4">Your cart is empty</p>
              <Link to="/shop" className="text-purple-600 font-semibold">
                Continue Shopping
              </Link>
            </div>
          ) : (
            <div className="bg-white rounded-lg shadow-md p-8">
              {guest.items.map((item) => (
                <div key={item.productId} className="flex items-center gap-4 py-4 border-b">
                  {item.imageUrl && (
                    <img src={item.imageUrl} alt={item.name} className="w-20 h-20 object-cover rounded" />
                  )}
                  <div className="flex-1">
                    <h3 className="font-semibold">{item.name}</h3>
                    <p className="text-purple-600 font-bold">${item.price.toFixed(2)}</p>
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      onClick={() => guest.updateQuantity(item.productId, item.quantity - 1)}
                      className="px-3 py-1 border rounded"
                    >
                      -
                    </button>
                    <span>{item.quantity}</span>
                    <button
                      onClick={() => guest.updateQuantity(item.productId, item.quantity + 1)}
                      className="px-3 py-1 border rounded"
                    >
                      +
                    </button>
                  </div>
                  <p className="font-semibold w-24 text-right">
                    ${(item.price * item.quantity).toFixed(2)}
                  </p>
                  <button
                    onClick={() => guest.removeItem(item.productId)}
                    className="text-red-500 hover:text-red-700"
                  >
                    Remove
                  </button>
                </div>
              ))}
              <div className="mt-8 text-right">
                <p className="text-2xl font-bold">Subtotal: ${guest.subtotal().toFixed(2)}</p>
                <p className="text-sm text-gray-500 mt-1">Shipping &amp; tax calculated at checkout</p>
                <Link
                  to="/login"
                  className="bg-purple-600 text-white px-8 py-3 rounded-lg font-semibold hover:bg-purple-700 transition inline-block mt-4"
                >
                  Log in to Checkout
                </Link>
              </div>
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">Shopping Cart</h1>
        {items.length === 0 ? (
          <div className="text-center py-12">
            <p className="text-gray-600 mb-4">Your cart is empty</p>
            <Link to="/shop" className="text-purple-600 font-semibold">
              Continue Shopping
            </Link>
          </div>
        ) : (
          <div className="bg-white rounded-lg shadow-md p-8">
            {items.map((item) => (
              <div key={item.id} className="flex items-center gap-4 py-4 border-b">
                <img src={item.productImage} alt={item.productName} className="w-20 h-20 object-cover rounded" />
                <div className="flex-1">
                  <h3 className="font-semibold">{item.productName}</h3>
                  <p className="text-purple-600 font-bold">${item.unitPrice.toFixed(2)}</p>
                </div>
                <div className="flex items-center gap-2">
                  <button
                    onClick={() => updateItem(item.productId, Math.max(1, item.quantity - 1))}
                    className="px-3 py-1 border rounded"
                  >
                    -
                  </button>
                  <span>{item.quantity}</span>
                  <button
                    onClick={() => updateItem(item.productId, item.quantity + 1)}
                    className="px-3 py-1 border rounded"
                  >
                    +
                  </button>
                </div>
                <p className="font-semibold w-24 text-right">${item.totalPrice.toFixed(2)}</p>
                <button
                  onClick={() => removeItem(item.productId)}
                  className="text-red-500 hover:text-red-700"
                >
                  Remove
                </button>
              </div>
            ))}
            <div className="mt-8 text-right">
              <p className="text-2xl font-bold">Total: ${total.toFixed(2)}</p>
              <Link
                to="/checkout"
                className="bg-purple-600 text-white px-8 py-3 rounded-lg font-semibold hover:bg-purple-700 transition inline-block mt-4"
              >
                Proceed to Checkout
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
