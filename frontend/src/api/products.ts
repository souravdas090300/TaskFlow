import api from './client';

export interface Product {
  id: string;
  name: string;
  slug: string;
  description: string;
  price: number;
  stock: number;
  categoryId: string;
  categoryName?: string;
  images: { id: string; imageUrl: string }[];
  variants?: { id: string; size: string; color: string; stock: number; price: number }[];
  isActive: boolean;
  featured: boolean;
  isNewArrival: boolean;
  createdAt: string;
}

export const productsApi = {
  getAll: async (params?: { page?: number; pageSize?: number; search?: string; category?: string; minPrice?: number; maxPrice?: number; sortBy?: string }) => {
    const response = await api.get('/products', { params });
    return response.data;
  },

  getById: async (id: string) => {
    const response = await api.get(`/products/${id}`);
    return response.data;
  },

  getBySlug: async (slug: string) => {
    const response = await api.get(`/products/slug/${slug}`);
    return response.data;
  },

  getFeatured: async (limit = 10) => {
    const response = await api.get('/products/featured', { params: { limit } });
    return response.data;
  },

  getNewArrivals: async (limit = 10) => {
    const response = await api.get('/products/new-arrivals', { params: { limit } });
    return response.data;
  },
};
