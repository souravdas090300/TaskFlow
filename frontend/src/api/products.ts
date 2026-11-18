import api from './client';

export interface ProductImage {
  id: string;
  imageUrl: string;
  altText?: string;
  displayOrder: number;
  isPrimary: boolean;
}

export interface ProductVariant {
  id: string;
  size?: string;
  color?: string;
  colorCode?: string;
  price: number;
  stock: number;
  sku?: string;
  isActive: boolean;
}

export interface Product {
  id: string;
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
  averageRating: number;
  reviewCount: number;
  createdAt: string;
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  images: ProductImage[];
  variants: ProductVariant[];
}

export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  price: number;
  stock: number;
  brand?: string;
  primaryImageUrl?: string;
  categoryName: string;
  categorySlug: string;
  isFeatured: boolean;
  isNewArrival: boolean;
  averageRating: number;
  reviewCount: number;
}

export interface Category {
  id: string;
  name: string;
  description?: string;
  imageUrl?: string;
  slug: string;
  parentCategoryId?: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  parentCategoryName?: string;
  subCategories?: Category[];
  productCount?: number;
}

export const productsApi = {
  getAll: async (params?: { page?: number; pageSize?: number; search?: string; category?: string; minPrice?: number; maxPrice?: number; sortBy?: string }): Promise<ProductListItem[]> => {
    const response = await api.get<ProductListItem[]>('/products', { params });
    return response.data;
  },

  getById: async (id: string): Promise<Product> => {
    const response = await api.get<Product>(`/products/${id}`);
    return response.data;
  },

  getBySlug: async (slug: string): Promise<Product> => {
    const response = await api.get<Product>(`/products/slug/${slug}`);
    return response.data;
  },

  getFeatured: async (limit = 10): Promise<ProductListItem[]> => {
    const response = await api.get<ProductListItem[]>('/products/featured', { params: { limit } });
    return response.data;
  },

  getNewArrivals: async (limit = 10): Promise<ProductListItem[]> => {
    const response = await api.get<ProductListItem[]>('/products/new-arrivals', { params: { limit } });
    return response.data;
  },
};

export const categoriesApi = {
  getAll: async (): Promise<Category[]> => {
    const response = await api.get<Category[]>('/categories');
    return response.data;
  },
};

export default productsApi;
