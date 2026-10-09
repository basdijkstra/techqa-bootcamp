import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Link, Route, Routes } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Layout } from './components/Layout';
import { AuthProvider } from './context/AuthContext';
import { BasketProvider } from './context/BasketContext';
import { BasketPage } from './pages/BasketPage';
import { CheckoutPaymentPage } from './pages/CheckoutPaymentPage';
import { CheckoutShippingPage } from './pages/CheckoutShippingPage';
import { GameDetailPage } from './pages/GameDetailPage';
import { HomePage } from './pages/HomePage';
import { OrderConfirmationPage, OrderLookupPage } from './pages/OrderPages';
import { AdminLayout, LoginPage } from './pages/admin/AdminLayout';
import { DashboardPage } from './pages/admin/DashboardPage';
import { OrderDetailPage } from './pages/admin/OrderDetailPage';
import { OrdersPage } from './pages/admin/OrdersPage';
import { StockPage } from './pages/admin/StockPage';
import './styles.css';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: { retry: 1, refetchOnWindowFocus: false, staleTime: 10_000 },
  },
});

function NotFoundPage() {
  return (
    <div className="empty" data-testid="not-found">
      <h1>Page not found</h1>
      <Link to="/">Back to the shop</Link>
    </div>
  );
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <BasketProvider>
          <BrowserRouter>
            <Routes>
              <Route element={<Layout />}>
                <Route index element={<HomePage />} />
                <Route path="games/:slug" element={<GameDetailPage />} />
                <Route path="basket" element={<BasketPage />} />
                <Route path="checkout/shipping" element={<CheckoutShippingPage />} />
                <Route path="checkout/payment" element={<CheckoutPaymentPage />} />
                <Route path="order-confirmation/:orderNumber" element={<OrderConfirmationPage />} />
                <Route path="orders/lookup" element={<OrderLookupPage />} />
                <Route path="admin/login" element={<LoginPage />} />
                <Route path="admin" element={<AdminLayout />}>
                  <Route index element={<DashboardPage />} />
                  <Route path="stock" element={<StockPage />} />
                  <Route path="orders" element={<OrdersPage />} />
                  <Route path="orders/:orderNumber" element={<OrderDetailPage />} />
                </Route>
                <Route path="*" element={<NotFoundPage />} />
              </Route>
            </Routes>
          </BrowserRouter>
        </BasketProvider>
      </AuthProvider>
    </QueryClientProvider>
  </StrictMode>,
);
