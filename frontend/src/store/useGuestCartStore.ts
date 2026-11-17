import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { cartApi } from '../api/cart';

export interface GuestCartItem {
  productId: string;
  name: string;
  price: number;
  imageUrl?: string;
  quantity: number;
}

interface GuestCartState {
  items: GuestCartItem[];
  addItem: (item: Omit<GuestCartItem, 'quantity'>, quantity?: number) => void;
  updateQuantity: (productId: string, quantity: number) => void;
  removeItem: (productId: string) => void;
  clear: () => void;
  totalItems: () => number;
  subtotal: () => number;
}

export const useGuestCartStore = create<GuestCartState>()(
  persist(
    (set, get) => ({
      items: [],

      addItem: (item, quantity = 1) => {
        set((state) => {
          const existing = state.items.find((i) => i.productId === item.productId);
          if (existing) {
            return {
              items: state.items.map((i) =>
                i.productId === item.productId ? { ...i, quantity: i.quantity + quantity } : i
              ),
            };
          }
          return { items: [...state.items, { ...item, quantity }] };
        });
      },

      updateQuantity: (productId, quantity) => {
        set((state) => ({
          items:
            quantity <= 0
              ? state.items.filter((i) => i.productId !== productId)
              : state.items.map((i) => (i.productId === productId ? { ...i, quantity } : i)),
        }));
      },

      removeItem: (productId) => {
        set((state) => ({ items: state.items.filter((i) => i.productId !== productId) }));
      },

      clear: () => set({ items: [] }),

      totalItems: () => get().items.reduce((sum, i) => sum + i.quantity, 0),

      subtotal: () => get().items.reduce((sum, i) => sum + i.price * i.quantity, 0),
    }),
    { name: 'guest-cart' }
  )
);

/**
 * Merges the anonymous guest cart into the authenticated user's server cart,
 * then clears the guest cart. Called after login/register.
 */
export async function mergeGuestCartIntoAccount(): Promise<void> {
  const guest = useGuestCartStore.getState();
  if (guest.items.length === 0) return;

  for (const item of guest.items) {
    try {
      await cartApi.addItem(item.productId, item.quantity);
    } catch {
      // Item may be unavailable — skip it rather than failing the whole merge
    }
  }
  guest.clear();
}
