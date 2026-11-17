import { useRef } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../api/orders';

export const OrderConfirmationPage = () => {
  const { id } = useParams<{ id: string }>();
  // NEVER trust the redirect params — poll until the webhook flips the status.
  const startedAt = useRef(Date.now());
  const { data: order, isLoading } = useQuery({
    queryKey: ['order', id],
    queryFn: () => ordersApi.getOrderById(id!),
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      const settled = status === 'Paid' || status === 'PaymentReview' || status === 'Cancelled';
      return settled || Date.now() - startedAt.current > 60_000 ? false : 2000;
    },
  });

  if (isLoading || !order) {
    return (
      <div className="min-h-screen bg-gray-50 py-8">
        <div className="container mx-auto px-4">
          <p>Loading your order...</p>
        </div>
      </div>
    );
  }

  let message: string;
  if (order.status === 'Paid') {
    message = `Payment received. Thank you! Your order ${order.orderNumber} is confirmed.`;
  } else if (order.status === 'PaymentReview') {
    message = `We received your payment but need to check stock for order ${order.orderNumber}. We will email you shortly. Please do not pay again.`;
  } else if (Date.now() - startedAt.current > 60_000) {
    message = 'Still processing. Check "My orders" in a minute. Please do not pay again.';
  } else {
    message = 'Confirming your payment...';
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4">
        <div className="bg-white rounded-lg shadow-md p-8 max-w-2xl mx-auto">
          <h1 className="text-2xl font-bold mb-4">Order Confirmation</h1>
          <p className="mb-6">{message}</p>
          <div className="flex gap-4">
            <Link to={`/orders/${order.id}`} className="text-purple-600 hover:underline">
              View order details
            </Link>
            <Link to="/orders" className="text-purple-600 hover:underline">
              My orders
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default OrderConfirmationPage;
