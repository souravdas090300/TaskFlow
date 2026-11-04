import { create } from 'zustand';
import { cartApi } from '../api/cart';
import type { CartItem } from '../api/cart';

interface CartState {
  items: CartItem[];
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
  fetchCart: () => Promise<void>;
  addItem: (productId: string, quantity: number) => Promise<void>;
  updateItem: (productId: string, quantity: number) => Promise<void>;
  removeItem: (productId: string) => Promise<void>;
  clearCart: () => Promise<void>;
}

export const useCartStore = create<CartState>((set) => ({
  items: [],
  subtotal: 0,
  tax: 0,
  shipping: 0,
  total: 0,

  fetchCart: async () => {
    const cart = await cartApi.getCart();
    set({
      items: cart.items,
      subtotal: cart.subtotal,
      tax: cart.tax,
      shipping: cart.shipping,
      total: cart.total,
    });
  },

  addItem: async (productId, quantity) => {
    await cartApi.addItem(productId, quantity);
    const cart = await cartApi.getCart();
    set({
      items: cart.items,
      subtotal: cart.subtotal,
      tax: cart.tax,
      shipping: cart.shipping,
      total: cart.total,
    });
  },

  updateItem: async (productId, quantity) => {
    await cartApi.updateItem(productId, quantity);
    const cart = await cartApi.getCart();
    set({
      items: cart.items,
      subtotal: cart.subtotal,
      tax: cart.tax,
      shipping: cart.shipping,
      total: cart.total,
    });
  },

  removeItem: async (productId) => {
    await cartApi.removeItem(productId);
    const cart = await cartApi.getCart();
    set({
      items: cart.items,
      subtotal: cart.subtotal,
      tax: cart.tax,
      shipping: cart.shipping,
      total: cart.total,
    });
  },

  clearCart: async () => {
    await cartApi.clearCart();
    set({ items: [], subtotal: 0, tax: 0, shipping: 0, total: 0 });
  },
}));
