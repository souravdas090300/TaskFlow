import api from './client';

export const startStripeCheckout = async (orderId: string): Promise<string> => {
  const response = await api.post<{ url: string }>('/stripe/checkout', { orderId });
  return response.data.url;
};

export const confirmCashOnDelivery = async (orderId: string): Promise<void> => {
  await api.post(`/orders/${orderId}/cash-on-delivery`);
};
