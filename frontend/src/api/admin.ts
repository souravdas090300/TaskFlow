import axios from 'axios';
import { clearAuthStorage, ensureFreshAccessToken } from './client';
import type { Category, Product, ProductListItem } from './products';

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
  (error) => {
    throw error;
  }
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
      throw error;
    }

    originalRequest._retried = true;
    try {
      await ensureFreshAccessToken();
      originalRequest.headers.Authorization = `Bearer ${localStorage.getItem('token')}`;
      return client(originalRequest);
    } catch {
      clearAdminAuthAndRedirect();
      throw error;
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

export interface ProductQueryParams {
  page?: number;
  pageSize?: number;
  search?: string;
  categoryId?: string;
  minPrice?: number;
  maxPrice?: number;
  sortBy?: string;
}

export interface ProductImagePayload {
  imageUrl: string;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductVariantPayload {
  size?: string;
  color?: string;
  colorCode?: string;
  price: number;
  stock: number;
  sku?: string;
  isActive: boolean;
}

export interface CreateProductPayload {
  name: string;
  description: string;
  slug: string;
  price: number;
  stock: number;
  brand?: string;
  sku?: string;
  isFeatured: boolean;
  isNewArrival: boolean;
  categoryId: string;
  images: ProductImagePayload[];
  variants: ProductVariantPayload[];
}

export interface UpdateProductPayload {
  name: string;
  description: string;
  slug: string;
  price: number;
  stock: number;
  brand?: string;
  sku?: string;
  isFeatured: boolean;
  isNewArrival: boolean;
  isActive: boolean;
  categoryId: string;
  images?: ProductImagePayload[];
  variants?: ProductVariantPayload[];
}

export interface CategoryPayload {
  name: string;
  description?: string;
  imageUrl?: string;
  slug: string;
  parentCategoryId?: string;
  displayOrder: number;
}

export interface UpdateCategoryPayload extends CategoryPayload {
  isActive: boolean;
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

  getProducts: async (params?: ProductQueryParams): Promise<ProductListItem[]> => {
    const response = await client.get<ProductListItem[]>('/api/products', { params });
    return response.data;
  },

  getProduct: async (id: string): Promise<Product> => {
    const response = await client.get<Product>(`/api/products/${id}`);
    return response.data;
  },

  createProduct: async (data: CreateProductPayload): Promise<Product> => {
    const response = await client.post<Product>('/api/products', data);
    return response.data;
  },

  updateProduct: async (id: string, data: UpdateProductPayload): Promise<Product> => {
    const response = await client.put<Product>(`/api/products/${id}`, data);
    return response.data;
  },

  deleteProduct: async (id: string): Promise<void> => {
    await client.delete(`/api/products/${id}`);
  },

  getCategories: async (): Promise<Category[]> => {
    const response = await client.get<Category[]>('/api/categories');
    return response.data;
  },

  createCategory: async (data: CategoryPayload): Promise<Category> => {
    const response = await client.post<Category>('/api/categories', data);
    return response.data;
  },

  updateCategory: async (id: string, data: UpdateCategoryPayload): Promise<Category> => {
    const response = await client.put<Category>(`/api/categories/${id}`, data);
    return response.data;
  },

  deleteCategory: async (id: string): Promise<void> => {
    await client.delete(`/api/categories/${id}`);
  },
};

export interface PaymentIssue {
  id: string;
  orderId?: string;
  orderNumber?: string;
  paymentIntentId: string;
  type: string;
  details: string;
  createdAt: string;
}

export default client;
