import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useGuestCartStore, mergeGuestCartIntoAccount } from './useGuestCartStore';
import { cartApi } from '../api/cart';

vi.mock('../api/cart', () => ({
  cartApi: {
    addItem: vi.fn().mockResolvedValue({}),
  },
}));

describe('useGuestCartStore', () => {
  beforeEach(() => {
    localStorage.clear();
    useGuestCartStore.setState({ items: [] });
  });

  it('adds a new item', () => {
    const { addItem } = useGuestCartStore.getState();
    addItem({ productId: 'p1', name: 'Widget', price: 10 });
    expect(useGuestCartStore.getState().items).toHaveLength(1);
    expect(useGuestCartStore.getState().items[0].quantity).toBe(1);
  });

  it('increments quantity for an existing product', () => {
    const { addItem } = useGuestCartStore.getState();
    addItem({ productId: 'p1', name: 'Widget', price: 10 });
    addItem({ productId: 'p1', name: 'Widget', price: 10 }, 2);
    expect(useGuestCartStore.getState().items[0].quantity).toBe(3);
  });

  it('updates and removes quantities', () => {
    const { addItem, updateQuantity, removeItem } = useGuestCartStore.getState();
    addItem({ productId: 'p1', name: 'Widget', price: 10 });
    updateQuantity('p1', 5);
    expect(useGuestCartStore.getState().items[0].quantity).toBe(5);
    updateQuantity('p1', 0);
    expect(useGuestCartStore.getState().items).toHaveLength(0);
    addItem({ productId: 'p2', name: 'Gadget', price: 20 });
    removeItem('p2');
    expect(useGuestCartStore.getState().items).toHaveLength(0);
  });

  it('computes totals', () => {
    const { addItem, totalItems, subtotal } = useGuestCartStore.getState();
    addItem({ productId: 'p1', name: 'Widget', price: 10 }, 2);
    addItem({ productId: 'p2', name: 'Gadget', price: 20 });
    expect(totalItems()).toBe(3);
    expect(subtotal()).toBe(40);
  });
});

describe('mergeGuestCartIntoAccount', () => {
  beforeEach(() => {
    localStorage.clear();
    useGuestCartStore.setState({ items: [] });
    vi.mocked(cartApi.addItem).mockClear();
  });

  it('pushes every guest item to the server cart and clears guest cart', async () => {
    const { addItem } = useGuestCartStore.getState();
    addItem({ productId: 'p1', name: 'Widget', price: 10 }, 2);
    addItem({ productId: 'p2', name: 'Gadget', price: 20 });

    await mergeGuestCartIntoAccount();

    expect(cartApi.addItem).toHaveBeenCalledWith('p1', 2);
    expect(cartApi.addItem).toHaveBeenCalledWith('p2', 1);
    expect(useGuestCartStore.getState().items).toHaveLength(0);
  });

  it('does nothing when the guest cart is empty', async () => {
    await mergeGuestCartIntoAccount();
    expect(cartApi.addItem).not.toHaveBeenCalled();
  });

  it('continues merging when one item fails', async () => {
    vi.mocked(cartApi.addItem)
      .mockRejectedValueOnce(new Error('out of stock'))
      .mockResolvedValueOnce({});
    const { addItem } = useGuestCartStore.getState();
    addItem({ productId: 'p1', name: 'Widget', price: 10 });
    addItem({ productId: 'p2', name: 'Gadget', price: 20 });

    await mergeGuestCartIntoAccount();

    expect(cartApi.addItem).toHaveBeenCalledTimes(2);
    expect(useGuestCartStore.getState().items).toHaveLength(0);
  });
});
