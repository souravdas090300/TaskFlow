import api from './client';

export interface CartItem {
  id: string;
  cartId: string;
  productId: string;
  productName: string;
  productImage?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

export interface Cart {
  id: string;
  userId: string;
  items: CartItem[];
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
}

export const cartApi = {
  getCart: async (): Promise<Cart> => {
    const response = await api.get('/cart');
    return response.data;
  },

  addItem: async (productId: string, quantity: number) => {
    const response = await api.post('/cart/items', { productId, quantity });
    return response.data;
  },

  updateItem: async (productId: string, quantity: number) => {
    const response = await api.put(`/cart/items/${productId}`, { quantity });
    return response.data;
  },

  removeItem: async (productId: string) => {
    const response = await api.delete(`/cart/items/${productId}`);
    return response.data;
  },

  clearCart: async () => {
    const response = await api.delete('/cart');
    return response.data;
  },
};
