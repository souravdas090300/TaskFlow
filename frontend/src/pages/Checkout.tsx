import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../api/orders';
import { useCartStore } from '../store/useCartStore';
import { useNavigate } from 'react-router-dom';

export const Checkout = () => {
  const { data: checkout } = useQuery({
    queryKey: ['checkout'],
    queryFn: () => ordersApi.getCheckout(),
  });

  const { clearCart } = useCartStore();
  const navigate = useNavigate();
  const [selectedAddress, setSelectedAddress] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('Stripe');

  const handleCheckout = async () => {
    try {
      const order = await ordersApi.createOrder({
        shippingAddressId: selectedAddress,
        paymentMethod,
      });
      clearCart();
      navigate(`/orders/${order.id}`);
    } catch (error) {
      alert('Checkout failed');
    }
  };

  if (!checkout) return <div>Loading...</div>;

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">Checkout</h1>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="bg-white rounded-lg shadow-md p-8">
            <h2 className="text-xl font-bold mb-4">Shipping Address</h2>
            <select
              value={selectedAddress}
              onChange={(e) => setSelectedAddress(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg mb-4"
              required
            >
              <option value="">Select an address</option>
              {checkout.addresses.map((addr: any) => (
                <option key={addr.id} value={addr.id}>
                  {addr.firstName} {addr.lastName} - {addr.addressLine1}
                </option>
              ))}
            </select>

            <h2 className="text-xl font-bold mb-4 mt-6">Payment Method</h2>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg"
            >
              <option value="Stripe">Credit Card (Stripe)</option>
              <option value="CashOnDelivery">Cash on Delivery</option>
            </select>

            <button
              onClick={handleCheckout}
              disabled={!selectedAddress}
              className="w-full bg-purple-600 text-white py-3 rounded-lg font-semibold hover:bg-purple-700 transition mt-6 disabled:bg-gray-400"
            >
              Place Order
            </button>
          </div>

          <div className="bg-white rounded-lg shadow-md p-8">
            <h2 className="text-xl font-bold mb-4">Order Summary</h2>
            {checkout.items.map((item) => (
              <div key={item.id} className="flex justify-between py-2 border-b">
                <span>{item.productName} x {item.quantity}</span>
                <span>${item.totalPrice.toFixed(2)}</span>
              </div>
            ))}
            <div className="mt-4 space-y-2">
              <div className="flex justify-between">
                <span>Subtotal:</span>
                <span>${checkout.subtotal.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span>Tax (18%):</span>
                <span>${checkout.tax.toFixed(2)}</span>
              </div>
              <div className="flex justify-between">
                <span>Shipping:</span>
                <span>${checkout.shipping.toFixed(2)}</span>
              </div>
              <div className="flex justify-between font-bold text-xl mt-4 pt-4 border-t">
                <span>Total:</span>
                <span>${checkout.total.toFixed(2)}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
