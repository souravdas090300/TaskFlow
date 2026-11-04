import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '../api/admin';
import type { OrderDto } from '../api/orders';
import { useState } from 'react';

export default function AdminOrders() {
  const queryClient = useQueryClient();
  const { data: orders, isLoading, error } = useQuery({
    queryKey: ['admin-orders'],
    queryFn: adminApi.getAllOrders,
  });

  const updateStatusMutation = useMutation({
    mutationFn: ({ orderId, status }: { orderId: string; status: string }) =>
      adminApi.updateOrderStatus(orderId, status),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
    },
  });

  const updateTrackingMutation = useMutation({
    mutationFn: ({ orderId, trackingNumber }: { orderId: string; trackingNumber: string }) =>
      adminApi.updateOrderTracking(orderId, trackingNumber),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
    },
  });

  const [trackingInputs, setTrackingInputs] = useState<Record<string, string>>({});

  if (isLoading) return <div className="p-8">Loading orders...</div>;
  if (error) return <div className="p-8 text-red-600">Error loading orders</div>;

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-8">Order Management</h1>

      <div className="bg-white rounded-lg shadow-md overflow-hidden">
        <table className="min-w-full">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Order #
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Customer
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Total
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {orders?.map((order: OrderDto) => (
              <tr key={order.id}>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">{order.orderNumber}</div>
                  <div className="text-sm text-gray-500">{new Date(order.createdAt).toLocaleDateString()}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm text-gray-900">{order.userEmail || 'N/A'}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="text-sm font-medium text-gray-900">${order.total.toFixed(2)}</div>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <select
                    value={order.status}
                    onChange={(e) => updateStatusMutation.mutate({ orderId: order.id, status: e.target.value })}
                    className="text-sm border rounded px-2 py-1"
                    disabled={updateStatusMutation.isPending}
                  >
                    <option value="Pending">Pending</option>
                    <option value="Paid">Paid</option>
                    <option value="Processing">Processing</option>
                    <option value="Shipped">Shipped</option>
                    <option value="Delivered">Delivered</option>
                    <option value="Cancelled">Cancelled</option>
                  </select>
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <div className="flex items-center space-x-2">
                    <input
                      type="text"
                      placeholder="Tracking #"
                      value={trackingInputs[order.id] || order.trackingNumber || ''}
                      onChange={(e) => setTrackingInputs({ ...trackingInputs, [order.id]: e.target.value })}
                      className="text-sm border rounded px-2 py-1 w-32"
                    />
                    <button
                      onClick={() =>
                        updateTrackingMutation.mutate({
                          orderId: order.id,
                          trackingNumber: trackingInputs[order.id] || order.trackingNumber || '',
                        })
                      }
                      className="text-sm bg-blue-600 text-white px-3 py-1 rounded hover:bg-blue-700"
                      disabled={updateTrackingMutation.isPending}
                    >
                      Save
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
