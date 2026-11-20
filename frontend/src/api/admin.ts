import axios from 'axios';
import { clearAuthStorage, ensureFreshAccessToken } from './client';

const API_URL = import.meta.env.VITE_API_URL || '';

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

function clearAdminAuthAndRedirect() {
  clearAuthStorage();
  if (window.location.pathname !== '/admin/login') {
    window.location.href = '/admin/login';
  }
}

// Response interceptor: on 401, try one token refresh then retry the request once
client.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;
    if (error.response?.status !== 401 || originalRequest?._retried) {
      if (error.response?.status === 401) clearAdminAuthAndRedirect();
      return Promise.reject(error);
    }

    originalRequest._retried = true;
    try {
      await ensureFreshAccessToken();
      originalRequest.headers.Authorization = `Bearer ${localStorage.getItem('token')}`;
      return client(originalRequest);
    } catch {
      clearAdminAuthAndRedirect();
      return Promise.reject(error);
    }
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

  getPaymentIssues: async (): Promise<PaymentIssue[]> => {
    const response = await client.get('/api/admin/payment-issues');
    return response.data;
  },

  resolvePaymentIssue: async (id: string, note: string): Promise<void> => {
    await client.post(`/api/admin/payment-issues/${id}/resolve`, { note });
  },

  refundOrder: async (orderId: string): Promise<void> => {
    await client.post(`/api/admin/orders/${orderId}/refund`);
  },
};

export interface PaymentIssue {
  id: string;
  orderId?: string;
  orderNumber?: string;
  paymentIntentId: string;
  type: 'OrderNotFound' | 'OrderClosed' | 'DoubleCharge' | 'StockConflict' | string;
  details: string;
  createdAt: string;
}

export default client;
