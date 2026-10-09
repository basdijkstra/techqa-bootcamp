import type { ReactNode } from 'react';
import { ApiError } from '../api/http';
import type { Pricing, StockStatus } from '../api/types';
import { formatPrice, orderStatusLabels, shortAgeRating, stockStatusLabels } from '../format';

/** Generated cover art: the game's two colors and its initials. */
export function GameCover({ title, primary, secondary, size = 'medium' }: {
  title: string;
  primary: string | null;
  secondary: string | null;
  size?: 'small' | 'medium' | 'large';
}) {
  const initials = title
    .split(/[\s:]+/)
    .filter((word) => /^[A-Za-z0-9]/.test(word) && !['of', 'the', 'The'].includes(word))
    .slice(0, 3)
    .map((word) => word[0].toUpperCase())
    .join('');
  const id = `cover-${title.replace(/\W/g, '')}-${size}`;
  const p = primary ?? '#333333';
  const s = secondary ?? '#999999';

  return (
    <svg className={`game-cover game-cover-${size}`} viewBox="0 0 300 400" role="img" aria-label={`Cover art for ${title}`}>
      <defs>
        <linearGradient id={id} x1="0" y1="0" x2="1" y2="1">
          <stop offset="0%" stopColor={p} />
          <stop offset="100%" stopColor={s} />
        </linearGradient>
      </defs>
      <rect width="300" height="400" fill={`url(#${id})`} />
      <circle cx="230" cy="90" r="120" fill={s} opacity="0.25" />
      <circle cx="40" cy="360" r="90" fill={p} opacity="0.35" />
      <text x="150" y="230" textAnchor="middle" fontSize="110" fontWeight="800" fill="#ffffff" opacity="0.9"
        fontFamily="system-ui, sans-serif">
        {initials}
      </text>
    </svg>
  );
}

export function PriceTag({ pricing }: { pricing: Pricing }) {
  return (
    <div className="price" data-testid="price">
      {pricing.isOnSale ? (
        <>
          <span className="price-current price-sale" data-testid="price-current">{formatPrice(pricing.price)}</span>
          <span className="price-original" data-testid="price-original">{formatPrice(pricing.basePrice)}</span>
          <span className="badge badge-sale" data-testid="sale-badge">-{pricing.salePercentage}%</span>
        </>
      ) : (
        <span className="price-current" data-testid="price-current">{formatPrice(pricing.price)}</span>
      )}
    </div>
  );
}

export function StockBadge({ status, quantity }: { status: StockStatus; quantity?: number }) {
  const label = status === 'LowStock' && quantity !== undefined ? `Only ${quantity} left` : stockStatusLabels[status];
  return (
    <span className={`badge badge-stock badge-${status}`} data-testid="stock-status" data-status={status}>
      {label}
    </span>
  );
}

export function RatingBadge({ rating }: { rating: string }) {
  return (
    <span className={`badge badge-rating rating-${rating}`} data-testid="age-rating" title={`ESRB rating ${shortAgeRating(rating)}`}>
      {shortAgeRating(rating)}
    </span>
  );
}

export function OrderStatusBadge({ status }: { status: string }) {
  return (
    <span className={`badge badge-order status-${status}`} data-testid="order-status" data-status={status}>
      {orderStatusLabels[status] ?? status}
    </span>
  );
}

export function Spinner({ label = 'Loading...' }: { label?: string }) {
  return (
    <div className="spinner-wrapper" role="status" data-testid="loading">
      <span className="spinner" aria-hidden="true" />
      <span>{label}</span>
    </div>
  );
}

/** Shows an error from a request. Validation errors per field are shown next to the fields instead. */
export function ErrorMessage({ error, testId = 'error-message' }: { error: unknown; testId?: string }) {
  if (!error) return null;
  const message = error instanceof ApiError
    ? error.message
    : error instanceof Error
      ? error.message
      : String(error);
  const code = error instanceof ApiError ? error.code : undefined;

  return (
    <div className="alert alert-error" role="alert" data-testid={testId} data-error-code={code}>
      {message}
    </div>
  );
}

export function Notice({ children, kind = 'info', testId }: { children: ReactNode; kind?: 'info' | 'warning' | 'success'; testId?: string }) {
  return (
    <div className={`alert alert-${kind}`} data-testid={testId} role={kind === 'warning' ? 'alert' : 'status'}>
      {children}
    </div>
  );
}

interface FieldProps {
  id: string;
  label: string;
  error?: string[] | string;
  hint?: string;
  children: ReactNode;
}

/** Label, input and error message. The error has the id "<id>-error" and a data-testid of the same name. */
export function Field({ id, label, error, hint, children }: FieldProps) {
  const errors = typeof error === 'string' ? [error] : error;
  return (
    <div className={`field ${errors?.length ? 'field-invalid' : ''}`}>
      <label htmlFor={id}>{label}</label>
      {children}
      {hint && <small className="field-hint">{hint}</small>}
      {errors?.length ? (
        <div className="field-error" id={`${id}-error`} data-testid={`${id}-error`}>
          {errors.join(' ')}
        </div>
      ) : null}
    </div>
  );
}
