import { BrowserRouter as Router, Routes, Route, useLocation } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ProtectedRoute } from './components/ProtectedRoute';
import { AdminRoute } from './components/AdminRoute';
import { AdminLayout } from './components/AdminLayout';
import { Navbar } from './components/Navbar';
import { DemoBanner } from './components/DemoBanner';
import { Home } from './pages/Home';
import { Shop } from './pages/Shop';
import { ProductDetail } from './pages/ProductDetail';
import { Cart } from './pages/Cart';
import { Checkout } from './pages/Checkout';
import { Orders } from './pages/Orders';
import { OrderDetail } from './pages/OrderDetail';
import OrderConfirmationPage from './pages/OrderConfirmationPage';
import { Login } from './pages/Login';
import { Register } from './pages/Register';
import { ForgotPassword } from './pages/ForgotPassword';
import { ResetPassword } from './pages/ResetPassword';
import { VerifyEmail } from './pages/VerifyEmail';
import { PrivacyPolicy } from './pages/PrivacyPolicy';
import { TermsOfService } from './pages/TermsOfService';
import AdminDashboard from './pages/AdminDashboard';
import { AdminLogin } from './pages/AdminLogin';
import AdminOrders from './pages/AdminOrders';
import AdminPaymentIssues from './pages/AdminPaymentIssues';
import AdminProducts from './pages/AdminProducts';
import AdminCategories from './pages/AdminCategories';
import { useAuthStore } from './store/useAuthStore';
import { applyTheme, useThemeStore } from './store/useThemeStore';
import { useEffect } from 'react';

const queryClient = new QueryClient();

function ConditionalNavbar() {
  const { pathname } = useLocation();
  // The admin area has its own AdminLayout navigation.
  if (pathname.startsWith('/admin')) return null;
  return <Navbar />;
}

function App() {
  const loadUser = useAuthStore((state) => state.loadUser);
  const mode = useThemeStore((state) => state.mode);

  useEffect(() => {
    void loadUser();
  }, [loadUser]);

  useEffect(() => {
    applyTheme(mode);
  }, [mode]);

  return (
    <QueryClientProvider client={queryClient}>
      <DemoBanner />
      <Router>
        <ConditionalNavbar />
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/shop" element={<Shop />} />
          <Route path="/products/:slug" element={<ProductDetail />} />
          <Route path="/cart" element={<Cart />} />
          <Route path="/checkout" element={
            <ProtectedRoute>
              <Checkout />
            </ProtectedRoute>
          } />
          <Route path="/orders" element={
            <ProtectedRoute>
              <Orders />
            </ProtectedRoute>
          } />
          <Route path="/orders/:id" element={
            <ProtectedRoute>
              <OrderDetail />
            </ProtectedRoute>
          } />
          <Route path="/orders/:id/confirmation" element={
            <ProtectedRoute>
              <OrderConfirmationPage />
            </ProtectedRoute>
          } />
          <Route path="/login" element={<Login />} />
          <Route path="/register" element={<Register />} />
          <Route path="/forgot-password" element={<ForgotPassword />} />
          <Route path="/reset-password" element={<ResetPassword />} />
          <Route path="/verify-email" element={<VerifyEmail />} />
          <Route path="/privacy-policy" element={<PrivacyPolicy />} />
          <Route path="/terms-of-service" element={<TermsOfService />} />
          <Route path="/admin/login" element={<AdminLogin />} />
          <Route path="/admin" element={
            <AdminRoute>
              <AdminLayout />
            </AdminRoute>
          }>
            <Route index element={<AdminDashboard />} />
            <Route path="orders" element={<AdminOrders />} />
            <Route path="products" element={<AdminProducts />} />
            <Route path="categories" element={<AdminCategories />} />
            <Route path="payment-issues" element={<AdminPaymentIssues />} />
          </Route>
        </Routes>
      </Router>
    </QueryClientProvider>
  );
}

export default App;
