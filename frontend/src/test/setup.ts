// Node 25 ships a broken built-in localStorage and jsdom's is unavailable in this setup.
// Provide a minimal in-memory Storage for zustand persist and tests.
const store = new Map<string, string>();

const memoryStorage: Storage = {
  get length() { return store.size; },
  clear: () => store.clear(),
  getItem: (key: string) => (store.has(key) ? store.get(key)! : null),
  key: (index: number) => Array.from(store.keys())[index] ?? null,
  removeItem: (key: string) => { store.delete(key); },
  setItem: (key: string, value: string) => { store.set(key, String(value)); },
};

Object.defineProperty(globalThis, 'localStorage', { value: memoryStorage, configurable: true });
try {
  Object.defineProperty(window, 'localStorage', { value: memoryStorage, configurable: true });
} catch {
  // window.localStorage may be non-configurable; globalThis stub is enough
}
