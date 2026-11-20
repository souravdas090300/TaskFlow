import { useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ordersApi } from '../api/orders';
import { confirmCashOnDelivery, startStripeCheckout } from '../api/stripe';
import { useCartStore } from '../store/useCartStore';
import { useNavigate } from 'react-router-dom';
import { formatPrice } from '../utils/format';
import { AddressForm } from '../components/AddressForm';
import { getApiErrorMessage } from '../utils/errors';
import type { AddressDto } from '../api/addresses';

export const Checkout = () => {
  const { data: checkout } = useQuery({
    queryKey: ['checkout'],
    queryFn: () => ordersApi.getCheckout(),
  });

  const queryClient = useQueryClient();
  const { clearCart } = useCartStore();
  const navigate = useNavigate();
  const [selectedAddress, setSelectedAddress] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('Stripe');
  const [showAddressForm, setShowAddressForm] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const shippingAddresses = (checkout?.addresses ?? []).filter((address) => address.type === 'Shipping');

  const preferredAddress = shippingAddresses.find((address) => address.isDefault) ?? shippingAddresses[0];
  const effectiveAddress = selectedAddress || preferredAddress?.id || '';

  const handleAddressSaved = (address: AddressDto) => {
    setSelectedAddress(address.id);
    setShowAddressForm(false);
    void queryClient.invalidateQueries({ queryKey: ['checkout'] });
  };

  const handleCheckout = async () => {
    if (isSubmitting) return;
    setIsSubmitting(true);
    setError('');
    try {
      const order = await ordersApi.createOrder({
        shippingAddressId: effectiveAddress,
        paymentMethod,
      });
      void clearCart();
      if (paymentMethod === 'CashOnDelivery') {
        await confirmCashOnDelivery(order.id);
        void navigate(`/orders/${order.id}/confirmation`);
      } else {
        const url = await startStripeCheckout(order.id);
        window.location.assign(url); // leave the SPA for Stripe's hosted page
      }
    } catch (checkoutError) {
      setError(getApiErrorMessage(checkoutError, 'Checkout failed'));
      setIsSubmitting(false);
    }
  };

  if (!checkout) return <div>Loading...</div>;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-slate-950 py-8">
      <div className="container mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">Checkout</h1>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="bg-white dark:bg-slate-900 rounded-lg shadow-md p-8 border border-gray-200 dark:border-white/10">
            <h2 className="text-xl font-bold mb-4">Shipping Address</h2>

            {shippingAddresses.length > 0 && (
              <>
                <select
                  value={effectiveAddress}
                  onChange={(e) => setSelectedAddress(e.target.value)}
                  className="w-full px-4 py-2 border rounded-lg mb-3"
                  required
                >
                  <option value="">Select an address</option>
                  {shippingAddresses.map((addr) => (
                    <option key={addr.id} value={addr.id}>
                      {addr.firstName} {addr.lastName} - {addr.addressLine1}
                    </option>
                  ))}
                </select>
                <button
                  type="button"
                  onClick={() => setShowAddressForm((value) => !value)}
                  className="text-sm font-semibold text-purple-600 hover:text-purple-700"
                >
                  {showAddressForm ? 'Hide new address form' : 'Add new address'}
                </button>
              </>
            )}

            {(showAddressForm || shippingAddresses.length === 0) && (
              <div className="mt-4 rounded-lg border border-gray-200 dark:border-white/10 p-4 bg-gray-50 dark:bg-slate-800/60">
                <AddressForm
                  defaultType="Shipping"
                  showType={false}
                  defaultIsDefault={shippingAddresses.length === 0}
                  submitLabel={shippingAddresses.length === 0 ? 'Save & use address' : 'Save address'}
                  onSaved={handleAddressSaved}
                  onCancel={shippingAddresses.length === 0 ? undefined : () => setShowAddressForm(false)}
                />
              </div>
            )}

            <h2 className="text-xl font-bold mb-4 mt-6">Payment Method</h2>
            <select
              value={paymentMethod}
              onChange={(e) => setPaymentMethod(e.target.value)}
              className="w-full px-4 py-2 border rounded-lg"
            >
              <option value="Stripe">Credit Card (Stripe)</option>
              <option value="CashOnDelivery">Cash on Delivery</option>
            </select>

            {error && (
              <div className="mt-4 bg-red-50 border border-red-200 text-red-700 rounded-lg px-4 py-3 text-sm">
                {error}
              </div>
            )}

            <button
              onClick={handleCheckout}
              disabled={!effectiveAddress || isSubmitting}
              className="w-full bg-purple-600 text-white py-3 rounded-lg font-semibold hover:bg-purple-700 transition mt-6 disabled:bg-gray-400"
            >
              {isSubmitting ? 'Placing order...' : 'Place Order'}
            </button>
          </div>

          <div className="bg-white dark:bg-slate-900 rounded-lg shadow-md p-8 border border-gray-200 dark:border-white/10">
            <h2 className="text-xl font-bold mb-4">Order Summary</h2>
            {checkout.items.map((item) => (
              <div key={item.id} className="flex justify-between py-2 border-b">
                <span>{item.productName} x {item.quantity}</span>
                <span>{formatPrice(item.totalPrice)}</span>
              </div>
            ))}
            <div className="mt-4 space-y-2">
              <div className="flex justify-between">
                <span>Subtotal:</span>
                <span>{formatPrice(checkout.subtotal)}</span>
              </div>
              <div className="flex justify-between">
                <span>Tax (18%):</span>
                <span>{formatPrice(checkout.tax)}</span>
              </div>
              <div className="flex justify-between">
                <span>Shipping:</span>
                <span>{formatPrice(checkout.shipping)}</span>
              </div>
              <div className="flex justify-between font-bold text-xl mt-4 pt-4 border-t">
                <span>Total:</span>
                <span>{formatPrice(checkout.total)}</span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
