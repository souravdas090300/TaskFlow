import api from './client';

export interface AddressDto {
  id: string;
  userId: string;
  type: 'Shipping' | 'Billing' | string;
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone?: string;
  isDefault: boolean;
  createdAt: string;
}

export interface AddressPayload {
  type: string;
  firstName: string;
  lastName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  state: string;
  postalCode: string;
  country: string;
  phone?: string;
  isDefault: boolean;
}

export const addressesApi = {
  list: async (): Promise<AddressDto[]> => {
    const response = await api.get('/addresses');
    return response.data;
  },

  create: async (data: AddressPayload): Promise<AddressDto> => {
    const response = await api.post('/addresses', data);
    return response.data;
  },

  update: async (id: string, data: AddressPayload): Promise<AddressDto> => {
    const response = await api.put(`/addresses/${id}`, data);
    return response.data;
  },

  remove: async (id: string): Promise<void> => {
    await api.delete(`/addresses/${id}`);
  },
};
