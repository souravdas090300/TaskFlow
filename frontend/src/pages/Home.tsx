import { useQuery } from '@tanstack/react-query';
import { productsApi, type ProductListItem } from '../api/products';
import { Link } from 'react-router-dom';
import heroImg from '../assets/hero.png';

const features = [
  {
    title: 'Secure Payments',
    description: '256-bit encrypted checkout powered by Stripe. Your card details never touch our servers.',
    icon: (
      <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M9 12.75 11.25 15 15 9.75m-3-7.036A11.959 11.959 0 0 1 3.598 6 11.99 11.99 0 0 0 3 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285Z" />
      </svg>
    ),
  },
  {
    title: 'Fast Shipping',
    description: 'Free shipping on orders over $50. Tracked, insured delivery to your door in days.',
    icon: (
      <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M8.25 18.75a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h6m-9 0H3.375a1.125 1.125 0 0 1-1.125-1.125V14.25m17.25 4.5a1.5 1.5 0 0 1-3 0m3 0a1.5 1.5 0 0 0-3 0m3 0h1.125c.621 0 1.129-.504 1.09-1.124a17.902 17.902 0 0 0-3.213-9.193 2.056 2.056 0 0 0-1.58-.86H14.25M16.5 18.75h-2.25m0-11.177v-.958c0-.568-.422-1.048-.987-1.106a48.554 48.554 0 0 0-10.026 0 1.106 1.106 0 0 0-.987 1.106v7.635m12-6.677v6.677m0 4.5v-4.5m0 0h-12" />
      </svg>
    ),
  },
  {
    title: 'Easy Returns',
    description: 'Changed your mind? 30-day hassle-free returns. No questions, no restocking fees.',
    icon: (
      <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M16.023 9.348h4.992v-.001M2.985 19.644v-4.992m0 0h4.992m-4.993 0 3.181 3.183a8.25 8.25 0 0 0 13.803-3.7M4.031 9.865a8.25 8.25 0 0 1 13.803-3.7l3.181 3.182m0-4.991v4.99" />
      </svg>
    ),
  },
  {
    title: '24/7 Support',
    description: 'Real humans, real help — chat, email or phone, any time you need us.',
    icon: (
      <svg className="w-7 h-7" fill="none" stroke="currentColor" strokeWidth={1.8} viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" d="M20.25 8.511c.884.284 1.5 1.128 1.5 2.097v4.286c0 1.136-.847 2.1-1.98 2.193-.34.027-.68.052-1.02.072v3.091l-3-3c-1.354 0-2.694-.055-4.02-.163a2.115 2.115 0 0 1-.825-.242m9.345-8.334a2.126 2.126 0 0 0-.476-.095 48.64 48.64 0 0 0-8.048 0c-1.131.094-1.976 1.057-1.976 2.192v4.286c0 .837.46 1.58 1.155 1.951m9.345-8.334V6.637c0-1.621-1.152-3.026-2.76-3.235A48.455 48.455 0 0 0 11.25 3c-2.115 0-4.198.137-6.24.402-1.608.209-2.76 1.614-2.76 3.235v6.226c0 1.621 1.152 3.026 2.76 3.235.577.075 1.157.14 1.74.194V21l4.155-4.155" />
      </svg>
    ),
  },
];

const ProductCard = ({ product }: { product: ProductListItem }) => (
  <Link
    to={`/products/${product.slug}`}
    className="group bg-slate-900 border border-white/10 rounded-2xl overflow-hidden hover:border-purple-500/50 hover:shadow-[0_0_40px_-10px_rgba(139,92,246,0.4)] transition-all duration-300"
  >
    <div className="relative h-56 overflow-hidden bg-slate-800">
      {product.primaryImageUrl ? (
        <img
          src={product.primaryImageUrl}
          alt={product.name}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
        />
      ) : (
        <div className="w-full h-full flex items-center justify-center text-slate-600 text-sm">No image</div>
      )}
      {product.isFeatured && (
        <span className="absolute top-3 left-3 bg-purple-600/90 backdrop-blur text-white text-xs font-semibold px-3 py-1 rounded-full">
          Featured
        </span>
      )}
    </div>
    <div className="p-5">
      <h3 className="font-semibold text-lg text-white group-hover:text-purple-300 transition-colors line-clamp-1">
        {product.name}
      </h3>
      <div className="flex items-center justify-between mt-2">
        <span className="text-purple-400 font-bold text-lg">${product.price.toFixed(2)}</span>
        <span className="text-slate-500 text-sm group-hover:text-purple-400 transition-colors">View →</span>
      </div>
    </div>
  </Link>
);

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
    <div className="bg-slate-950 text-slate-200">
      {/* ===== HERO ===== */}
      <section className="relative overflow-hidden">
        {/* Ambient glows */}
        <div className="absolute -top-40 -left-40 w-[500px] h-[500px] bg-purple-700/25 rounded-full blur-[120px] pointer-events-none" />
        <div className="absolute top-20 -right-40 w-[500px] h-[500px] bg-cyan-600/20 rounded-full blur-[120px] pointer-events-none" />
        <div className="absolute bottom-0 left-1/3 w-[400px] h-[400px] bg-fuchsia-700/15 rounded-full blur-[100px] pointer-events-none" />

        <div className="container mx-auto px-4 pt-20 pb-24 relative">
          <div className="grid lg:grid-cols-2 gap-12 items-center">
            <div>
              <div className="animate-fade-in-up inline-flex items-center gap-2 bg-purple-500/10 border border-purple-500/30 text-purple-300 text-sm font-medium px-4 py-1.5 rounded-full mb-6">
                <span className="w-2 h-2 bg-purple-400 rounded-full animate-pulse" />
                New season collection is live
              </div>
              <h1 className="animate-fade-in-up-delay-1 text-5xl lg:text-7xl font-extrabold text-white leading-[1.05] tracking-tight">
                Shop the future of{' '}
                <span className="bg-gradient-to-r from-purple-400 via-fuchsia-400 to-cyan-400 bg-clip-text text-transparent">
                  great products
                </span>
              </h1>
              <p className="animate-fade-in-up-delay-2 mt-6 text-lg text-slate-400 max-w-lg">
                Curated essentials, honest prices, and checkout that just works —
                powered by Stripe, delivered fast.
              </p>
              <div className="animate-fade-in-up-delay-3 mt-8 flex flex-wrap gap-4">
                <Link
                  to="/shop"
                  className="bg-gradient-to-r from-purple-600 to-fuchsia-600 text-white px-8 py-3.5 rounded-full font-semibold hover:from-purple-500 hover:to-fuchsia-500 transition shadow-[0_0_30px_-5px_rgba(168,85,247,0.6)]"
                >
                  Shop Now
                </Link>
                <Link
                  to="/shop"
                  className="border border-white/15 text-white px-8 py-3.5 rounded-full font-semibold hover:bg-white/5 transition"
                >
                  Explore Collections
                </Link>
              </div>
              <div className="animate-fade-in-up-delay-3 mt-10 flex gap-8 text-sm">
                {[
                  ['10k+', 'Happy customers'],
                  ['4.9★', 'Average rating'],
                  ['24h', 'Dispatch time'],
                ].map(([num, label]) => (
                  <div key={label}>
                    <div className="text-2xl font-bold text-white">{num}</div>
                    <div className="text-slate-500">{label}</div>
                  </div>
                ))}
              </div>
            </div>

            {/* Hero visual */}
            <div className="relative hidden lg:block">
              <div className="absolute inset-0 bg-gradient-to-tr from-purple-600/30 to-cyan-500/20 rounded-[2rem] blur-2xl" />
              <img
                src={heroImg}
                alt="TaskFlow hero"
                className="relative w-full rounded-[2rem] border border-white/10 shadow-2xl animate-float"
              />
              {/* Floating cards */}
              <div className="absolute -bottom-6 -left-6 bg-slate-900/90 backdrop-blur border border-white/10 rounded-2xl px-5 py-4 shadow-xl animate-float" style={{ animationDelay: '1.5s' }}>
                <div className="text-xs text-slate-500">Order #TF-2481</div>
                <div className="text-emerald-400 font-semibold text-sm mt-1">✓ Payment confirmed</div>
              </div>
              <div className="absolute -top-6 -right-6 bg-slate-900/90 backdrop-blur border border-white/10 rounded-2xl px-5 py-4 shadow-xl animate-float" style={{ animationDelay: '3s' }}>
                <div className="text-xs text-slate-500">Free shipping</div>
                <div className="text-white font-semibold text-sm mt-1">Unlocked at $50</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* ===== MARQUEE ===== */}
      <div className="border-y border-white/10 bg-slate-900/60 overflow-hidden py-4">
        <div className="flex gap-12 whitespace-nowrap animate-marquee w-max">
          {[...Array(2)].map((_, dup) => (
            <div key={dup} className="flex gap-12">
              {['Free shipping over $50', '30-day returns', 'Secure Stripe checkout', 'New arrivals weekly', '24/7 customer support', 'Cash on delivery available'].map((t) => (
                <span key={t} className="flex items-center gap-3 text-sm text-slate-500">
                  <span className="w-1.5 h-1.5 bg-purple-500 rounded-full" />
                  {t}
                </span>
              ))}
            </div>
          ))}
        </div>
      </div>

      {/* ===== FEATURES ===== */}
      <section className="container mx-auto px-4 py-24">
        <div className="text-center max-w-2xl mx-auto mb-14">
          <h2 className="text-4xl font-bold text-white tracking-tight">Why shop with TaskFlow</h2>
          <p className="mt-4 text-slate-400">Everything you need for a seamless shopping experience — nothing you don't.</p>
        </div>
        <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {features.map((f) => (
            <div
              key={f.title}
              className="group bg-slate-900 border border-white/10 rounded-2xl p-6 hover:border-purple-500/40 hover:-translate-y-1 transition-all duration-300"
            >
              <div className="w-12 h-12 rounded-xl bg-gradient-to-br from-purple-600/20 to-cyan-500/20 border border-purple-500/30 flex items-center justify-center text-purple-300 group-hover:scale-110 transition-transform">
                {f.icon}
              </div>
              <h3 className="mt-4 text-lg font-semibold text-white">{f.title}</h3>
              <p className="mt-2 text-sm text-slate-400 leading-relaxed">{f.description}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ===== FEATURED PRODUCTS ===== */}
      {featuredProducts && featuredProducts.length > 0 && (
        <section className="container mx-auto px-4 pb-24">
          <div className="flex items-end justify-between mb-10">
            <div>
              <h2 className="text-4xl font-bold text-white tracking-tight">Featured Products</h2>
              <p className="mt-2 text-slate-400">Hand-picked by our team this week.</p>
            </div>
            <Link to="/shop" className="hidden sm:inline-flex text-purple-400 font-semibold hover:text-purple-300 transition">
              View all →
            </Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {featuredProducts.slice(0, 6).map((p) => <ProductCard key={p.id} product={p} />)}
          </div>
        </section>
      )}

      {/* ===== NEW ARRIVALS ===== */}
      {newArrivals && newArrivals.length > 0 && (
        <section className="container mx-auto px-4 pb-24">
          <div className="flex items-end justify-between mb-10">
            <div>
              <h2 className="text-4xl font-bold text-white tracking-tight">New Arrivals</h2>
              <p className="mt-2 text-slate-400">Fresh off the shelf — just landed.</p>
            </div>
            <Link to="/shop" className="hidden sm:inline-flex text-purple-400 font-semibold hover:text-purple-300 transition">
              View all →
            </Link>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {newArrivals.slice(0, 6).map((p) => <ProductCard key={p.id} product={p} />)}
          </div>
        </section>
      )}

      {/* ===== CTA ===== */}
      <section className="container mx-auto px-4 pb-28">
        <div className="relative overflow-hidden rounded-[2.5rem] bg-gradient-to-r from-purple-700 via-fuchsia-700 to-purple-800 px-8 py-16 text-center">
          <div className="absolute -top-20 -right-20 w-72 h-72 bg-white/10 rounded-full blur-3xl" />
          <div className="absolute -bottom-20 -left-20 w-72 h-72 bg-cyan-400/20 rounded-full blur-3xl" />
          <h2 className="relative text-4xl font-bold text-white tracking-tight">Ready to find your next favorite?</h2>
          <p className="relative mt-4 text-purple-100 max-w-xl mx-auto">
            Join thousands of happy customers. Checkout takes under a minute — card or cash on delivery.
          </p>
          <Link
            to="/shop"
            className="relative mt-8 inline-block bg-white text-purple-700 px-10 py-3.5 rounded-full font-bold hover:bg-purple-50 transition shadow-xl"
          >
            Start Shopping
          </Link>
        </div>
      </section>

      {/* ===== FOOTER ===== */}
      <footer className="border-t border-white/10 bg-slate-950">
        <div className="container mx-auto px-4 py-8 flex flex-col sm:flex-row items-center justify-between gap-4 text-sm text-slate-500">
          <p>© {new Date().getFullYear()} TaskFlow. All rights reserved.</p>
          <div className="flex gap-6">
            <Link to="/privacy-policy" className="hover:text-purple-400 transition">
              Privacy Policy
            </Link>
            <Link to="/terms-of-service" className="hover:text-purple-400 transition">
              Terms of Service
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
};

