import { request } from './http';
import type {
  AdminOrder,
  Address,
  Basket,
  Category,
  Country,
  GameDetail,
  GameSummary,
  LoginResponse,
  Order,
  OrderListItem,
  OrderPreview,
  OrderSummary,
  PagedResult,
  ShippingQuote,
  StaffUser,
  StockAdjustment,
  StockItem,
} from './types';

export interface GameSearchParams {
  category?: string[];
  platform?: string;
  minPrice?: string;
  maxPrice?: string;
  onSale?: boolean;
  inStockOnly?: boolean;
  search?: string;
  sort?: string;
  page?: number;
  pageSize?: number;
}

export const catalogApi = {
  searchGames: (params: GameSearchParams) =>
    request<PagedResult<GameSummary>>('catalog', '/games', { query: { ...params } }),
  getGameBySlug: (slug: string) => request<GameDetail>('catalog', `/games/by-slug/${encodeURIComponent(slug)}`),
  getCategories: () => request<Category[]>('catalog', '/categories'),
  getPlatforms: () => request<string[]>('catalog', '/platforms'),
  getStock: () => request<StockItem[]>('catalog', '/admin/stock', { auth: true }),
  getStockHistory: (gameId: number) =>
    request<StockAdjustment[]>('catalog', `/admin/games/${gameId}/stock-adjustments`, { auth: true }),
  adjustStock: (gameId: number, body: { quantityChange: number; reason: string; note?: string }) =>
    request<StockAdjustment>('catalog', `/admin/games/${gameId}/stock-adjustments`, { method: 'POST', body, auth: true }),
};

export const basketApi = {
  create: () => request<Basket>('basket', '/baskets', { method: 'POST' }),
  get: (id: string) => request<Basket>('basket', `/baskets/${id}`),
  addItem: (id: string, gameId: number, quantity: number) =>
    request<Basket>('basket', `/baskets/${id}/items`, { method: 'POST', body: { gameId, quantity } }),
  updateItem: (id: string, gameId: number, quantity: number) =>
    request<Basket>('basket', `/baskets/${id}/items/${gameId}`, { method: 'PUT', body: { quantity } }),
  removeItem: (id: string, gameId: number) =>
    request<Basket>('basket', `/baskets/${id}/items/${gameId}`, { method: 'DELETE' }),
  applyPromoCode: (id: string, code: string) =>
    request<Basket>('basket', `/baskets/${id}/promo-code`, { method: 'PUT', body: { code } }),
  removePromoCode: (id: string) => request<Basket>('basket', `/baskets/${id}/promo-code`, { method: 'DELETE' }),
};

export const shippingApi = {
  getCountries: () => request<Country[]>('shipping', '/countries'),
  createQuote: (body: { address: Address; itemCount: number; orderSubtotal: number; shipNotBefore: string | null }) =>
    request<ShippingQuote>('shipping', '/quotes', { method: 'POST', body }),
  getQuote: (id: string) => request<ShippingQuote>('shipping', `/quotes/${id}`),
};

export interface PlaceOrderBody {
  basketId: string;
  customer: { fullName: string; email: string; phone: string };
  shippingQuoteId: string;
  shippingMethod: string;
  payment: { number: string; cardholderName: string; expiryMonth: number; expiryYear: number; cvv: string };
  ageVerification?: { dateOfBirth: string | null };
}

export interface OrderListParams {
  status?: string;
  from?: string;
  to?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export const ordersApi = {
  preview: (body: { basketId: string; shippingQuoteId: string; shippingMethod: string }) =>
    request<OrderPreview>('orders', '/orders/preview', { method: 'POST', body }),
  placeOrder: (body: PlaceOrderBody) => request<Order>('orders', '/orders', { method: 'POST', body }),
  getOrder: (orderNumber: string, email: string) =>
    request<Order>('orders', `/orders/${encodeURIComponent(orderNumber)}`, { query: { email } }),
  cancelOrder: (orderNumber: string, email: string, reason?: string) =>
    request<Order>('orders', `/orders/${encodeURIComponent(orderNumber)}/cancel`, { method: 'POST', body: { email, reason } }),
  adminList: (params: OrderListParams) =>
    request<PagedResult<OrderListItem>>('orders', '/admin/orders', { query: { ...params }, auth: true }),
  adminSummary: () => request<OrderSummary>('orders', '/admin/orders/summary', { auth: true }),
  adminGet: (orderNumber: string) =>
    request<AdminOrder>('orders', `/admin/orders/${encodeURIComponent(orderNumber)}`, { auth: true }),
  adminChangeStatus: (orderNumber: string, body: { status: string; trackingNumber?: string; note?: string }) =>
    request<AdminOrder>('orders', `/admin/orders/${encodeURIComponent(orderNumber)}/status`, { method: 'POST', body, auth: true }),
};

export const identityApi = {
  login: (username: string, password: string) =>
    request<LoginResponse>('identity', '/auth/login', { method: 'POST', body: { username, password } }),
  me: () => request<StaffUser>('identity', '/auth/me', { auth: true }),
};
