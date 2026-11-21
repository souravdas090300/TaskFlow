import { useQuery } from '@tanstack/react-query';
import { useParams, Link } from 'react-router-dom';
import { productsApi } from '../api/products';
import { useCartStore } from '../store/useCartStore';
import { useGuestCartStore } from '../store/useGuestCartStore';
import { useAuthStore } from '../store/useAuthStore';
import { formatPrice } from '../utils/format';

export const ProductDetail = () => {
  const { slug } = useParams<{ slug: string }>();
  const { data: product } = useQuery({
    queryKey: ['product', slug],
    queryFn: () => productsApi.getBySlug(slug!),
  });

  const addItem = useCartStore((state) => state.addItem);

  const addToCart = async () => {
    if (!product) return;
    try {
      if (useAuthStore.getState().isAuthenticated) {
        await addItem(product.id, 1);
        alert('Added to cart!');
      } else {
        useGuestCartStore.getState().addItem({
          productId: product.id,
          name: product.name,
          price: product.price,
          imageUrl: product.images[0]?.imageUrl,
        });
        alert('Added to cart! Log in to check out.');
      }
    } catch (error) {
      const data = (error as { response?: { data?: unknown } })?.response?.data;
      console.error('Add to cart failed', data ?? error);
      alert('Could not add to cart');
    }
  };

  if (!product) return <div>Loading...</div>;

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-slate-950 py-8">
      <div className="container mx-auto px-4">
        <Link to="/shop" className="text-purple-600 mb-4 inline-block">
          ← Back to Shop
        </Link>
        <div className="bg-white dark:bg-slate-900 rounded-lg shadow-md p-8 mt-4 border border-gray-200 dark:border-white/10">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              {product.images[0] && (
                <img
                  src={product.images[0].imageUrl}
                  alt={product.name}
                  className="w-full rounded-lg"
                />
              )}
            </div>
            <div>
              <h1 className="text-3xl font-bold mb-4">{product.name}</h1>
              <p className="text-3xl text-purple-600 dark:text-purple-400 font-bold mb-4">
                {formatPrice(product.price)}
              </p>
              <p className="text-gray-600 mb-4">{product.description}</p>
              <p className="text-sm text-gray-500 mb-4">Stock: {product.stock}</p>
              <button
                onClick={addToCart}
                disabled={product.stock === 0}
                className="bg-purple-600 text-white px-8 py-3 rounded-lg font-semibold hover:bg-purple-700 transition disabled:bg-gray-400"
              >
                {product.stock === 0 ? 'Out of Stock' : 'Add to Cart'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
