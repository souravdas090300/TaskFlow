import { useQuery } from '@tanstack/react-query';
import type { ReactNode } from 'react';
import { useSearchParams } from 'react-router-dom';
import { categoriesApi, productsApi } from '../api/products';
import { ProductCard } from '../components/ProductCard';

export const Shop = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.get('search') ?? '';
  const category = searchParams.get('categoryId') ?? '';

  const setParam = (key: string, value: string) => {
    const next = new URLSearchParams(searchParams);
    if (value) next.set(key, value);
    else next.delete(key);
    setSearchParams(next, { replace: true });
  };

  const { data: products, isLoading } = useQuery({
    queryKey: ['products', search, category],
    queryFn: () =>
      productsApi.getAll({
        page: 1,
        pageSize: 100,
        search: search || undefined,
        categoryId: category || undefined,
      }),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  });

  let content: ReactNode;
  if (isLoading) {
    content = <div className="text-gray-600 dark:text-slate-400">Loading products...</div>;
  } else if (!products || products.length === 0) {
    content = (
      <div className="bg-white dark:bg-slate-900 border border-gray-200 dark:border-white/10 rounded-2xl p-12 text-center text-gray-600 dark:text-slate-400">
        No products found.
      </div>
    );
  } else {
    content = (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {products.map((product) => <ProductCard key={product.id} product={product} />)}
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-slate-950 py-10">
      <div className="container mx-auto px-4">
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-gray-900 dark:text-white">Shop</h1>
          <p className="mt-2 text-gray-600 dark:text-slate-400">Browse the full catalog.</p>
        </div>

        <div className="mb-8 flex gap-3 flex-wrap">
          <input
            type="text"
            placeholder="Search products..."
            value={search}
            onChange={(event) => setParam('search', event.target.value)}
            className="px-4 py-2 border border-gray-300 dark:border-white/15 bg-white dark:bg-slate-900 text-gray-900 dark:text-white rounded-lg flex-1 max-w-md"
          />
          <select
            value={category}
            onChange={(event) => setParam('categoryId', event.target.value)}
            className="px-4 py-2 border border-gray-300 dark:border-white/15 bg-white dark:bg-slate-900 text-gray-900 dark:text-white rounded-lg"
          >
            <option value="">All categories</option>
            {categories?.map((cat) => (
              <option key={cat.id} value={cat.id}>
                {cat.name}
              </option>
            ))}
          </select>
        </div>

        {content}
      </div>
    </div>
  );
};
