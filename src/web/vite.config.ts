import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// The browser calls every PixelVault service directly. When started by Aspire, the service URLs come from
// environment variables (CATALOG_HTTP, BASKET_HTTP, ...). Otherwise the fixed local ports are used.
const serviceUrl = (name: string, port: number) =>
  JSON.stringify(process.env[`${name.toUpperCase()}_HTTP`] ?? `http://localhost:${port}`);

export default defineConfig({
  plugins: [react()],
  define: {
    __CATALOG_URL__: serviceUrl('catalog', 5101),
    __BASKET_URL__: serviceUrl('basket', 5102),
    __ORDERS_URL__: serviceUrl('orders', 5103),
    __PAYMENT_URL__: serviceUrl('payment', 5104),
    __SHIPPING_URL__: serviceUrl('shipping', 5105),
    __IDENTITY_URL__: serviceUrl('identity', 5106),
  },
  server: {
    port: Number(process.env.PORT ?? 5173),
    strictPort: true,
  },
});
