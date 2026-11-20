import { useState } from 'react';
import { Link } from 'react-router-dom';
import type { ProductListItem } from '../api/products';
import { useAuthStore } from '../store/useAuthStore';
import { useCartStore } from '../store/useCartStore';
import { useGuestCartStore } from '../store/useGuestCartStore';
import { formatPrice } from '../utils/format';

export const ProductCard = ({ product }: { product: ProductListItem }) => {
  const [added, setAdded] = useState(false);
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const addItem = useCartStore((state) => state.addItem);

  const handleAdd = async (event: React.MouseEvent) => {
    event.preventDefault();
    event.stopPropagation();
    if (product.stock <= 0) return;
    if (isAuthenticated) {
      await addItem(product.id, 1);
    } else {
      useGuestCartStore.getState().addItem({
        productId: product.id,
        name: product.name,
        price: product.price,
        imageUrl: product.primaryImageUrl,
      });
    }
    setAdded(true);
    window.setTimeout(() => setAdded(false), 1200);
  };

  return (
    <Link
      to={`/products/${product.slug}`}
      className="group bg-white dark:bg-slate-900 border border-gray-200 dark:border-white/10 rounded-2xl overflow-hidden hover:border-purple-400 dark:hover:border-purple-500/60 hover:shadow-lg transition-all duration-300"
    >
      <div className="relative h-56 overflow-hidden bg-gray-100 dark:bg-slate-800">
        {product.primaryImageUrl ? (
          <img src={product.primaryImageUrl} alt={product.name} className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500" />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-400 dark:text-slate-600 text-sm">No image</div>
        )}
        {product.stock === 1 && (
          <span className="absolute top-3 left-3 bg-red-600 text-white text-xs font-semibold px-3 py-1 rounded-full">Only 1 left</span>
        )}
        {product.isFeatured && product.stock !== 1 && (
          <span className="absolute top-3 left-3 bg-purple-600 text-white text-xs font-semibold px-3 py-1 rounded-full">Featured</span>
        )}
      </div>
      <div className="p-5">
        <p className="text-xs uppercase tracking-wide text-gray-500 dark:text-slate-400">{product.categoryName}</p>
        <h3 className="mt-1 font-semibold text-lg text-gray-900 dark:text-white group-hover:text-purple-700 dark:group-hover:text-purple-300 transition-colors line-clamp-1">
          {product.name}
        </h3>
        <div className="mt-3 flex items-center justify-between gap-3">
          <span className="text-purple-700 dark:text-purple-400 font-bold text-lg">{formatPrice(product.price)}</span>
          <button
            type="button"
            onClick={handleAdd}
            disabled={product.stock <= 0}
            className="bg-purple-600 text-white text-sm font-semibold px-4 py-2 rounded-lg hover:bg-purple-700 disabled:bg-gray-300 dark:disabled:bg-slate-700 transition"
          >
            {product.stock <= 0 ? 'Sold out' : added ? 'Added' : 'Add'}
          </button>
        </div>
      </div>
    </Link>
  );
};
