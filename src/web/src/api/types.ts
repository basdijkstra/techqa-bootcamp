// Types for the JSON returned by the PixelVault services.

export type StockStatus = 'InStock' | 'LowStock' | 'OutOfStock' | 'PreOrder';
export type AgeRating = 'E' | 'E10Plus' | 'T' | 'M' | 'AO';

export interface Pricing {
  basePrice: number;
  price: number;
  isOnSale: boolean;
  salePercentage: number | null;
  savings: number;
  saleEndsAt: string | null;
}

export interface GameSummary {
  id: number;
  slug: string;
  title: string;
  category: string;
  platforms: string[];
  ageRating: AgeRating;
  minimumAge: number;
  releaseDate: string;
  pricing: Pricing;
  stockStatus: StockStatus;
  availableQuantity: number;
  coverColorPrimary: string;
  coverColorSecondary: string;
}

export interface GameDetail extends GameSummary {
  description: string;
  publisher: string;
  developer: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Category {
  name: string;
  displayName: string;
  gameCount: number;
}

export interface BasketLine {
  gameId: number;
  slug: string;
  title: string;
  category: string;
  ageRating: AgeRating;
  minimumAge: number;
  isPreOrder: boolean;
  releaseDate: string | null;
  quantity: number;
  unitPrice: number;
  unitPriceWhenAdded: number;
  priceChanged: boolean;
  lineTotal: number;
  stockStatus: StockStatus;
  availableQuantity: number;
  coverColorPrimary: string | null;
  coverColorSecondary: string | null;
  issues: string[];
}

export interface PromoCodeStatus {
  code: string;
  description: string | null;
  isValid: boolean;
  isApplied: boolean;
  discount: number;
  failureReason: string | null;
  message: string;
}

export interface Basket {
  id: string;
  status: 'Active' | 'CheckedOut';
  createdAt: string;
  lastActivityAt: string;
  expiresAt: string;
  orderReference: string | null;
  items: BasketLine[];
  itemCount: number;
  subtotal: number;
  bundleDiscount: number;
  promoCode: PromoCodeStatus | null;
  appliedDiscount: 'None' | 'Bundle' | 'PromoCode';
  discountTotal: number;
  discountDescription: string | null;
  total: number;
  minimumAge: number;
  latestReleaseDate: string | null;
  hasIssues: boolean;
  canCheckout: boolean;
  messages: string[];
}

export interface Region {
  code: string;
  name: string;
}

export interface Country {
  code: string;
  name: string;
  regionLabel: string;
  postalCodeLabel: string;
  postalCodeExample: string;
  regions: Region[];
}

export interface Address {
  line1: string;
  line2?: string | null;
  city: string;
  region: string;
  postalCode: string;
  country: string;
}

export type ShippingMethod = 'Standard' | 'Express';

export interface ShippingOption {
  method: ShippingMethod;
  cost: number;
  isFree: boolean;
  shipDate: string;
  estimatedDeliveryDate: string;
  transitBusinessDays: number;
}

export interface ShippingQuote {
  id: string;
  createdAt: string;
  expiresAt: string;
  isExpired: boolean;
  address: Address;
  zone: string;
  itemCount: number;
  orderSubtotal: number;
  shipNotBefore: string | null;
  options: ShippingOption[];
  notices: string[];
}

export interface OrderPreview {
  subtotal: number;
  discountTotal: number;
  discountDescription: string | null;
  shippingMethod: string;
  shippingCost: number;
  taxRate: number;
  taxAmount: number;
  total: number;
  estimatedShipDate: string;
  estimatedDeliveryDate: string;
  minimumAge: number;
  requiresAgeVerification: boolean;
}

export type OrderStatus = 'Pending' | 'PaymentFailed' | 'Paid' | 'Shipped' | 'Delivered' | 'Cancelled' | 'Refunded';

export interface OrderLine {
  gameId: number;
  title: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  isPreOrder: boolean;
  releaseDate: string | null;
}

export interface Order {
  orderNumber: string;
  status: OrderStatus;
  createdAt: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  shippingAddress: Address;
  shippingMethod: string;
  estimatedShipDate: string | null;
  estimatedDeliveryDate: string | null;
  lines: OrderLine[];
  subtotal: number;
  discountTotal: number;
  discountDescription: string | null;
  shippingCost: number;
  taxRate: number;
  taxAmount: number;
  total: number;
  cardBrand: string | null;
  cardLast4: string | null;
  trackingNumber: string | null;
  paidAt: string | null;
  shippedAt: string | null;
  deliveredAt: string | null;
  cancelledAt: string | null;
  refundedAt: string | null;
  cancellationReason: string | null;
  canBeCancelledByCustomer: boolean;
  cancellationDeadline: string | null;
}

export interface OrderStatusChange {
  fromStatus: OrderStatus | null;
  toStatus: OrderStatus;
  changedAt: string;
  changedBy: string;
  note: string | null;
}

export interface AdminOrder {
  order: Order;
  basketId: string;
  paymentId: string | null;
  paymentFailureCode: string | null;
  allowedStatusChanges: OrderStatus[];
  history: OrderStatusChange[];
}

export interface OrderListItem {
  orderNumber: string;
  status: OrderStatus;
  createdAt: string;
  customerName: string;
  customerEmail: string;
  shippingCountry: string;
  shippingRegion: string;
  itemCount: number;
  total: number;
}

export interface OrderSummary {
  totalOrders: number;
  countByStatus: Record<OrderStatus, number>;
  ordersToday: number;
  awaitingShipment: number;
  revenueLast7Days: number;
  revenueAllTime: number;
}

export interface StockItem {
  gameId: number;
  title: string;
  category: string;
  stockQuantity: number;
  stockStatus: StockStatus;
  releaseDate: string;
  price: number;
}

export interface StockAdjustment {
  id: number;
  gameId: number;
  quantityChange: number;
  quantityBefore: number;
  quantityAfter: number;
  reason: string;
  note: string | null;
  performedBy: string;
  performedAt: string;
}

export interface StaffUser {
  username: string;
  displayName: string;
  role: 'Admin' | 'Support';
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  user: StaffUser;
}
