import { useQuery } from '@tanstack/react-query';
import { adminApi } from '../api/admin';
import { Link } from 'react-router-dom';

export default function AdminDashboard() {
  const { data: stats, isLoading, error } = useQuery({
    queryKey: ['admin-stats'],
    queryFn: adminApi.getStats,
  });

  if (isLoading) return <div className="p-8">Loading dashboard...</div>;
  if (error) return <div className="p-8 text-red-600">Error loading dashboard</div>;

  return (
    <div className="p-8">
      <h1 className="text-3xl font-bold mb-8">Admin Dashboard</h1>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Total Orders</h3>
          <p className="text-3xl font-bold text-gray-900">{stats?.totalOrders || 0}</p>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Total Revenue</h3>
          <p className="text-3xl font-bold text-green-600">${stats?.totalRevenue?.toFixed(2) || '0.00'}</p>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Pending Orders</h3>
          <p className="text-3xl font-bold text-yellow-600">{stats?.pendingOrders || 0}</p>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Low Stock Products</h3>
          <p className="text-3xl font-bold text-red-600">{stats?.lowStockProducts || 0}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Processing</h3>
          <p className="text-2xl font-bold text-blue-600">{stats?.processingOrders || 0}</p>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Shipped</h3>
          <p className="text-2xl font-bold text-purple-600">{stats?.shippedOrders || 0}</p>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Delivered</h3>
          <p className="text-2xl font-bold text-green-600">{stats?.deliveredOrders || 0}</p>
        </div>

        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium">Cancelled</h3>
          <p className="text-2xl font-bold text-gray-600">{stats?.cancelledOrders || 0}</p>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="bg-white p-6 rounded-lg shadow-md">
          <h3 className="text-gray-600 text-sm font-medium mb-2">Total Products</h3>
          <p className="text-2xl font-bold text-gray-900">{stats?.totalProducts || 0}</p>
        </div>

        <Link
          to="/admin/orders"
          className="bg-blue-600 text-white p-6 rounded-lg shadow-md hover:bg-blue-700 transition-colors flex items-center justify-center"
        >
          <span className="text-xl font-bold">Manage Orders</span>
        </Link>
      </div>
    </div>
  );
}
