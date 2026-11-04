import { useQuery } from '@tanstack/react-query';
import { productsApi } from '../api/products';
import { Link } from 'react-router-dom';

export const Home = () => {
  const { data: featuredProducts } = useQuery({
    queryKey: ['featured-products'],
    queryFn: () => productsApi.getFeatured(6),
  });

  const { data: newArrivals } = useQuery({
    queryKey: ['new-arrivals'],
    queryFn: () => productsApi.getNewArrivals(6),
  });

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Hero Section */}
      <div className="bg-gradient-to-r from-purple-600 to-blue-600 text-white py-20">
        <div className="container mx-auto px-4 text-center">
          <h1 className="text-5xl font-bold mb-4">Welcome to TaskFlow</h1>
          <p className="text-xl mb-8">Your premium e-commerce destination</p>
          <Link
            to="/shop"
            className="bg-white text-purple-600 px-8 py-3 rounded-full font-semibold hover:bg-gray-100 transition"
          >
            Shop Now
          </Link>
        </div>
      </div>

      {/* Featured Products */}
      <div className="container mx-auto px-4 py-16">
        <h2 className="text-3xl font-bold mb-8">Featured Products</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {featuredProducts?.map((product: any) => (
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
                <p className="text-purple-600 font-bold">${product.price.toFixed(2)}</p>
              </div>
            </Link>
          ))}
        </div>
      </div>

      {/* New Arrivals */}
      <div className="container mx-auto px-4 py-16">
        <h2 className="text-3xl font-bold mb-8">New Arrivals</h2>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {newArrivals?.map((product: any) => (
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
                <p className="text-purple-600 font-bold">${product.price.toFixed(2)}</p>
              </div>
            </Link>
          ))}
        </div>
      </div>
    </div>
  );
};
