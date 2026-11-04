import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../api/orders';
import { Link } from 'react-router-dom';

export const Orders = () => {
  const { data: orders } = useQuery({
    queryKey: ['orders'],
    queryFn: () => ordersApi.getOrders(),
  });

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4">
        <h1 className="text-3xl font-bold mb-8">My Orders</h1>
        {orders?.length === 0 ? (
          <p className="text-gray-600">No orders yet</p>
        ) : (
          <div className="space-y-4">
            {orders?.map((order) => (
              <Link
                key={order.id}
                to={`/orders/${order.id}`}
                className="bg-white rounded-lg shadow-md p-6 block hover:shadow-lg transition"
              >
                <div className="flex justify-between items-center">
                  <div>
                    <h3 className="font-semibold text-lg">{order.orderNumber}</h3>
                    <p className="text-gray-600 text-sm">{new Date(order.createdAt).toLocaleDateString()}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-bold text-purple-600">${order.total.toFixed(2)}</p>
                    <span className={`text-sm px-2 py-1 rounded ${
                      order.status === 'Paid' ? 'bg-green-100 text-green-800' :
                      order.status === 'Pending' ? 'bg-yellow-100 text-yellow-800' :
                      order.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                      'bg-blue-100 text-blue-800'
                    }`}>
                      {order.status}
                    </span>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};
