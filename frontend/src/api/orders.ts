import api from './client';
import type { CartItem } from './cart';

export interface OrderItem {
  id: string;
  orderId: string;
  productId: string;
  productName: string;
  productImage?: string;
  quantity: number;
  unitPrice: number;
  total: number;
}

export interface Order {
  id: string;
  userId: string;
  orderNumber: string;
  status: 'Pending' | 'Paid' | 'Processing' | 'Shipped' | 'Delivered' | 'Cancelled';
  paymentMethod: 'Stripe' | 'CashOnDelivery';
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
  stripePaymentIntentId?: string;
  stripeEventId?: string;
  createdAt: string;
  paidAt?: string;
  shippedAt?: string;
  deliveredAt?: string;
  cancelledAt?: string;
  shippingAddress?: any;
  billingAddress?: any;
  items: OrderItem[];
}

export interface Checkout {
  items: CartItem[];
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
  addresses: any[];
}

export const ordersApi = {
  getCheckout: async (): Promise<Checkout> => {
    const response = await api.get('/checkout');
    return response.data;
  },

  createOrder: async (data: { shippingAddressId: string; billingAddressId?: string; paymentMethod: string }): Promise<Order> => {
    const response = await api.post('/orders', data);
    return response.data;
  },

  getOrders: async (): Promise<Order[]> => {
    const response = await api.get('/orders');
    return response.data;
  },

  getOrderById: async (id: string): Promise<Order> => {
    const response = await api.get(`/orders/${id}`);
    return response.data;
  },

  cancelOrder: async (id: string) => {
    const response = await api.post(`/orders/${id}/cancel`);
    return response.data;
  },
};
