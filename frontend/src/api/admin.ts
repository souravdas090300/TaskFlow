import axios from 'axios';

const API_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const client = axios.create({
  baseURL: API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to add auth token
client.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor to handle errors
client.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export interface AdminStats {
  totalOrders: number;
  totalRevenue: number;
  pendingOrders: number;
  processingOrders: number;
  shippedOrders: number;
  deliveredOrders: number;
  cancelledOrders: number;
  totalProducts: number;
  lowStockProducts: number;
}

export const adminApi = {
  getStats: async (): Promise<AdminStats> => {
    const response = await client.get('/api/admin/stats');
    return response.data;
  },

  getAllOrders: async () => {
    const response = await client.get('/api/admin/orders');
    return response.data;
  },

  updateOrderStatus: async (orderId: string, status: string) => {
    const response = await client.put(`/api/admin/orders/${orderId}/status`, null, {
      params: { status },
    });
    return response.data;
  },

  updateOrderTracking: async (orderId: string, trackingNumber: string) => {
    const response = await client.put(`/api/admin/orders/${orderId}/tracking`, null, {
      params: { trackingNumber },
    });
    return response.data;
  },
};

export default client;
