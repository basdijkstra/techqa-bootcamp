import type { Address, ShippingMethod } from '../api/types';

// Checkout progress is kept in session storage so a page refresh doesn't lose it.
// Card details are never stored.

export interface CustomerDetails {
  fullName: string;
  email: string;
  phone: string;
}

export interface CheckoutState {
  basketId: string;
  customer: CustomerDetails;
  address: Address;
  quoteId: string;
  shippingMethod: ShippingMethod;
}

const CHECKOUT_KEY = 'pv.checkout';
const LAST_ORDER_KEY = 'pv.lastOrder';

export function loadCheckout(): CheckoutState | null {
  const stored = sessionStorage.getItem(CHECKOUT_KEY);
  return stored ? (JSON.parse(stored) as CheckoutState) : null;
}

export function saveCheckout(state: CheckoutState) {
  sessionStorage.setItem(CHECKOUT_KEY, JSON.stringify(state));
}

export function clearCheckout() {
  sessionStorage.removeItem(CHECKOUT_KEY);
}

export function saveLastOrder(orderNumber: string, email: string) {
  sessionStorage.setItem(LAST_ORDER_KEY, JSON.stringify({ orderNumber, email }));
}

export function loadLastOrder(): { orderNumber: string; email: string } | null {
  const stored = sessionStorage.getItem(LAST_ORDER_KEY);
  return stored ? JSON.parse(stored) : null;
}
