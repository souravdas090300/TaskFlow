import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../api/orders';
import { Link } from 'react-router-dom';
import { formatPrice } from '../utils/format';

export const Orders = () => {
  const { data: orders } = useQuery({
    queryKey: ['orders'],
    queryFn: () => ordersApi.getOrders(),
  });

  const getStatusClass = (status: string) => {
    if (status === 'Paid') return 'bg-green-100 text-green-800';
    if (status === 'Pending') return 'bg-yellow-100 text-yellow-800';
    if (status === 'Cancelled') return 'bg-red-100 text-red-800';
    return 'bg-blue-100 text-blue-800';
  };

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-slate-950 py-8">
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
                className="bg-white dark:bg-slate-900 rounded-lg shadow-md p-6 block border border-gray-200 dark:border-white/10 hover:shadow-lg transition"
              >
                <div className="flex justify-between items-center">
                  <div>
                    <h3 className="font-semibold text-lg">{order.orderNumber}</h3>
                    <p className="text-gray-600 text-sm">{new Date(order.createdAt).toLocaleDateString()}</p>
                  </div>
                  <div className="text-right">
                    <p className="font-bold text-purple-600">{formatPrice(order.total)}</p>
                    <span className={`text-sm px-2 py-1 rounded ${getStatusClass(order.status)}`}>
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
