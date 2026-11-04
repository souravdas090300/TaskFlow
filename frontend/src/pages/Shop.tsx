import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../api/products';
import type { Product } from '../api/products';
import { Link } from 'react-router-dom';
import { useState } from 'react';

export const Shop = () => {
  const [search, setSearch] = useState('');
  const [category, setCategory] = useState('');

  const { data: products } = useQuery({
    queryKey: ['products', search, category],
    queryFn: () => productsApi.getAll({ search, category }),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: async () => {
      const response = await fetch('http://localhost:5000/api/categories');
      return response.json();
    },
  });

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="container mx-auto px-4">
        <h1 className="text-4xl font-bold mb-8">Shop</h1>

        {/* Filters */}
        <div className="mb-8 flex gap-4 flex-wrap">
          <input
            type="text"
            placeholder="Search products..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="px-4 py-2 border rounded-lg flex-1 max-w-md"
          />
          <select
            value={category}
            onChange={(e) => setCategory(e.target.value)}
            className="px-4 py-2 border rounded-lg"
          >
            <option value="">All Categories</option>
            {categories?.map((cat: any) => (
              <option key={cat.id} value={cat.id}>
                {cat.name}
              </option>
            ))}
          </select>
        </div>

        {/* Products Grid */}
        <div className="grid grid-cols-1 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {products?.map((product: Product) => (
            <Link
              key={product.id}
              to={`/products/${product.slug}`}
              className="bg-white rounded-lg shadow-md overflow-hidden hover:shadow-lg transition"
            >
              {product.images[0] && (
                <img
                  src={product.images[0].imageUrl}
                  alt={product.name}
                  className="w-full h-48 object-cover"
                />
              )}
              <div className="p-4">
                <h3 className="font-semibold text-lg mb-2">{product.name}</h3>
                <p className="text-gray-600 text-sm mb-2">{product.categoryName}</p>
                <p className="text-purple-600 font-bold">${product.price.toFixed(2)}</p>
                <p className="text-sm text-gray-500">Stock: {product.stock}</p>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
};
