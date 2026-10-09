const currency = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' });

export const formatPrice = (amount: number) => currency.format(amount);

/** Formats a date-only value ("2026-10-09") without time zone shifts. */
export function formatDate(value: string | null | undefined): string {
  if (!value) return '';
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  return new Date(year, month - 1, day).toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  });
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) return '';
  return new Date(value).toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  });
}

export const formatPercent = (rate: number) =>
  `${(rate * 100).toLocaleString('en-US', { maximumFractionDigits: 3 })}%`;

export const ageRatingLabels: Record<string, string> = {
  E: 'E - Everyone',
  E10Plus: 'E10+ - Everyone 10+',
  T: 'T - Teen',
  M: 'M - Mature 17+',
  AO: 'AO - Adults Only 18+',
};

export const shortAgeRating = (rating: string) => (rating === 'E10Plus' ? 'E10+' : rating);

export const categoryLabel = (category: string) => (category === 'Rpg' ? 'RPG' : category);

export const stockStatusLabels: Record<string, string> = {
  InStock: 'In stock',
  LowStock: 'Low stock',
  OutOfStock: 'Out of stock',
  PreOrder: 'Pre-order',
};

export const orderStatusLabels: Record<string, string> = {
  Pending: 'Pending',
  PaymentFailed: 'Payment failed',
  Paid: 'Paid',
  Shipped: 'Shipped',
  Delivered: 'Delivered',
  Cancelled: 'Cancelled',
  Refunded: 'Refunded',
};

/** Formats a 10 digit phone number as (212) 555-0123. */
export function formatPhone(phone: string): string {
  return phone.length === 10 ? `(${phone.slice(0, 3)}) ${phone.slice(3, 6)}-${phone.slice(6)}` : phone;
}
