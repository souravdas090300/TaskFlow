import { useParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../api/orders';
import { Link } from 'react-router-dom';

export const OrderDetail = () => {
  const { id } = useParams<{ id: string }>();
  const { data: order } = useQuery({
    queryKey: ['order', id],
    queryFn: () => ordersApi.getOrderById(id!),
  });

  if (!order) return <div>Loading...</div>;

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4">
        <Link to="/orders" className="text-purple-600 mb-4 inline-block">
          ← Back to Orders
        </Link>
        <div className="bg-white rounded-lg shadow-md p-8 mt-4">
          <div className="flex justify-between items-start mb-6">
            <div>
              <h1 className="text-2xl font-bold">{order.orderNumber}</h1>
              <p className="text-gray-600">{new Date(order.createdAt).toLocaleString()}</p>
            </div>
            <span className={`px-3 py-1 rounded-full text-sm font-semibold ${
              order.status === 'Paid' ? 'bg-green-100 text-green-800' :
              order.status === 'Pending' ? 'bg-yellow-100 text-yellow-800' :
              order.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
              'bg-blue-100 text-blue-800'
            }`}>
              {order.status}
            </span>
          </div>

          <div className="mb-6">
            <h2 className="text-lg font-semibold mb-2">Items</h2>
            {order.items.map((item) => (
              <div key={item.id} className="flex justify-between py-2 border-b">
                <span>{item.productName} x {item.quantity}</span>
                <span>${item.total.toFixed(2)}</span>
              </div>
            ))}
          </div>

          <div className="border-t pt-4">
            <div className="flex justify-between mb-2">
              <span>Subtotal:</span>
              <span>${order.subtotal.toFixed(2)}</span>
            </div>
            <div className="flex justify-between mb-2">
              <span>Tax:</span>
              <span>${order.tax.toFixed(2)}</span>
            </div>
            <div className="flex justify-between mb-2">
              <span>Shipping:</span>
              <span>${order.shipping.toFixed(2)}</span>
            </div>
            <div className="flex justify-between font-bold text-xl mt-4 pt-4 border-t">
              <span>Total:</span>
              <span>${order.total.toFixed(2)}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
