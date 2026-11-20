import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { categoriesApi, productsApi } from '../api/products';
import { ProductCard } from '../components/ProductCard';
import { formatPrice, FREE_SHIPPING_THRESHOLD } from '../utils/format';
import heroImg from '../assets/hero.png';

const trustItems = [
  { title: 'Secure checkout', text: 'Payments are handled by Stripe; card details never touch our servers.' },
  { title: 'Free shipping', text: `On orders over ${formatPrice(FREE_SHIPPING_THRESHOLD)}.` },
  { title: 'Cash on delivery', text: 'Pay when your order arrives.' },
  { title: 'Tracked delivery', text: 'Every order gets a tracking number.' },
];

const shopPoints = [
  { title: 'Curated catalog', text: 'A focused mix of desk, kitchen, sneaker and everyday-carry picks.' },
  { title: 'Honest pricing', text: 'Clear EUR prices, VAT and shipping shown before you pay.' },
  { title: 'Fast checkout', text: 'Stripe for cards, cash on delivery when you prefer.' },
];

export const Home = () => {
  const { data: featuredProducts } = useQuery({
    queryKey: ['featured-products'],
    queryFn: () => productsApi.getFeatured(8),
  });

  const { data: newArrivals } = useQuery({
    queryKey: ['new-arrivals'],
    queryFn: () => productsApi.getNewArrivals(8),
  });

  const { data: categories } = useQuery({
    queryKey: ['categories'],
    queryFn: categoriesApi.getAll,
  });

  return (
    <div className="bg-gray-50 dark:bg-slate-950 text-gray-900 dark:text-slate-200">
      <section className="relative overflow-hidden bg-white dark:bg-slate-900 border-b border-gray-200 dark:border-white/10">
        <div className="absolute -top-32 left-1/4 w-[420px] h-[420px] bg-purple-200/50 dark:bg-purple-700/20 rounded-full blur-3xl" />
        <div className="container mx-auto px-4 py-16 lg:py-24 relative">
          <div className="grid lg:grid-cols-2 gap-10 items-center">
            <div>
              <div className="inline-flex items-center gap-2 bg-purple-100 dark:bg-purple-500/10 border border-purple-200 dark:border-purple-500/30 text-purple-800 dark:text-purple-300 text-sm font-medium px-4 py-1.5 rounded-full mb-6">
                <span className="w-2 h-2 bg-purple-500 rounded-full" />
                Demo store — test payments only
              </div>
              <h1 className="text-4xl sm:text-5xl lg:text-6xl font-extrabold tracking-tight text-gray-900 dark:text-white leading-[1.08]">
                Good finds for <span className="text-purple-700 dark:text-purple-400">everyday life</span>
              </h1>
              <p className="mt-5 text-lg text-gray-600 dark:text-slate-400 max-w-xl">
                Desk setups, kitchen essentials, sneakers and everyday carry — curated, fairly priced, and checkout by Stripe or cash on delivery.
              </p>
              <div className="mt-8 flex flex-wrap gap-3">
                <Link to="/shop" className="bg-purple-600 text-white px-7 py-3.5 rounded-xl font-semibold hover:bg-purple-700 transition">
                  Shop now
                </Link>
                <Link to="/shop" className="border border-gray-300 dark:border-white/20 text-gray-900 dark:text-white px-7 py-3.5 rounded-xl font-semibold hover:bg-gray-100 dark:hover:bg-white/5 transition">
                  Browse categories
                </Link>
              </div>
            </div>
            <div className="relative">
              <div className="absolute inset-0 bg-purple-200/60 dark:bg-purple-600/20 rounded-3xl blur-2xl" />
              <img src={heroImg} alt="Mercato hero" className="relative w-full rounded-3xl border border-gray-200 dark:border-white/10 shadow-2xl" />
            </div>
          </div>
        </div>
      </section>

      {categories && categories.length > 0 && (
        <section className="container mx-auto px-4 py-14">
          <div className="flex items-end justify-between mb-8">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white">Shop by category</h2>
              <p className="mt-2 text-gray-600 dark:text-slate-400">Jump straight to the section you need.</p>
            </div>
            <Link to="/shop" className="text-purple-700 dark:text-purple-400 font-semibold hover:underline">View all →</Link>
          </div>
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {categories.slice(0, 8).map((category) => (
              <Link
                key={category.id}
                to={`/shop?categoryId=${category.id}`}
                className="bg-white dark:bg-slate-900 border border-gray-200 dark:border-white/10 rounded-2xl p-6 hover:border-purple-400 dark:hover:border-purple-500/60 hover:shadow-md transition"
              >
                <h3 className="font-semibold text-gray-900 dark:text-white">{category.name}</h3>
                <p className="mt-1 text-sm text-gray-500 dark:text-slate-400">{category.productCount ?? 0} products</p>
              </Link>
            ))}
          </div>
        </section>
      )}

      {featuredProducts && featuredProducts.length > 0 && (
        <section className="container mx-auto px-4 py-14">
          <div className="flex items-end justify-between mb-8">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white">Featured products</h2>
              <p className="mt-2 text-gray-600 dark:text-slate-400">Hand-picked picks from across the store.</p>
            </div>
            <Link to="/shop" className="text-purple-700 dark:text-purple-400 font-semibold hover:underline">View all →</Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {featuredProducts.slice(0, 8).map((product) => <ProductCard key={product.id} product={product} />)}
          </div>
        </section>
      )}

      <section className="container mx-auto px-4 py-10">
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          {trustItems.map((item) => (
            <div key={item.title} className="bg-white dark:bg-slate-900 border border-gray-200 dark:border-white/10 rounded-2xl p-5">
              <h3 className="font-semibold text-gray-900 dark:text-white">{item.title}</h3>
              <p className="mt-1 text-sm text-gray-600 dark:text-slate-400">{item.text}</p>
            </div>
          ))}
        </div>
      </section>

      {newArrivals && newArrivals.length > 0 && (
        <section className="container mx-auto px-4 py-14">
          <div className="flex items-end justify-between mb-8">
            <div>
              <h2 className="text-3xl font-bold text-gray-900 dark:text-white">New arrivals</h2>
              <p className="mt-2 text-gray-600 dark:text-slate-400">Fresh additions to the catalog.</p>
            </div>
            <Link to="/shop" className="text-purple-700 dark:text-purple-400 font-semibold hover:underline">View all →</Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {newArrivals.slice(0, 8).map((product) => <ProductCard key={product.id} product={product} />)}
          </div>
        </section>
      )}

      <section className="container mx-auto px-4 py-14">
        <div className="bg-purple-700 rounded-3xl p-8 lg:p-12 text-white">
          <div className="grid lg:grid-cols-3 gap-8">
            {shopPoints.map((point) => (
              <div key={point.title}>
                <h3 className="text-lg font-semibold">{point.title}</h3>
                <p className="mt-2 text-purple-100">{point.text}</p>
              </div>
            ))}
          </div>
          <div className="mt-10">
            <Link to="/shop" className="inline-block bg-white text-purple-700 px-8 py-3.5 rounded-xl font-bold hover:bg-purple-50 transition">
              Browse all products
            </Link>
          </div>
        </div>
      </section>

      <footer className="border-t border-gray-200 dark:border-white/10 bg-white dark:bg-slate-950">
        <div className="container mx-auto px-4 py-10 grid gap-8 md:grid-cols-3 text-sm">
          <div>
            <p className="font-bold text-lg text-gray-900 dark:text-white">Mercato</p>
            <p className="mt-2 text-gray-600 dark:text-slate-400">Demo storefront for a portfolio project. Test payments only.</p>
          </div>
          <div>
            <p className="font-semibold text-gray-900 dark:text-white mb-2">Legal</p>
            <div className="flex flex-col gap-2 text-gray-600 dark:text-slate-400">
              <Link to="/privacy-policy" className="hover:text-purple-700 dark:hover:text-purple-300">Privacy Policy</Link>
              <Link to="/terms-of-service" className="hover:text-purple-700 dark:hover:text-purple-300">Terms of Service</Link>
            </div>
          </div>
          <div>
            <p className="font-semibold text-gray-900 dark:text-white mb-2">Project</p>
            <div className="flex flex-col gap-2 text-gray-600 dark:text-slate-400">
              <a href="https://github.com/souravdas090300/taskflow" target="_blank" rel="noreferrer" className="hover:text-purple-700 dark:hover:text-purple-300">GitHub repo</a>
              <span>Built by Sourav</span>
            </div>
          </div>
        </div>
        <div className="border-t border-gray-200 dark:border-white/10">
          <div className="container mx-auto px-4 py-5 text-sm text-gray-500 dark:text-slate-500 flex flex-col sm:flex-row justify-between gap-2">
            <p>© {new Date().getFullYear()} Mercato. Demo project.</p>
            <p>Prices in EUR. Stripe test mode.</p>
          </div>
        </div>
      </footer>
    </div>
  );
};
