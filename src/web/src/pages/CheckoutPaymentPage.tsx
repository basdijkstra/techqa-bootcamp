import { useState, type FormEvent } from 'react';
import { Link, Navigate } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../api/api';
import { ApiError } from '../api/http';
import { CheckoutSteps } from '../components/CheckoutSteps';
import { ErrorMessage, Field, Notice, Spinner } from '../components/common';
import { useBasket } from '../context/BasketContext';
import { clearCheckout, loadCheckout, saveLastOrder } from '../context/checkoutStorage';
import { formatDate, formatPercent, formatPrice, formatPhone } from '../format';

const MONTHS = Array.from({ length: 12 }, (_, i) => i + 1);
const thisYear = new Date().getFullYear();
const YEARS = Array.from({ length: 12 }, (_, i) => thisYear - 1 + i);

/** Error codes that mean the customer must go back to the shipping step. */
const RESTART_SHIPPING_CODES = ['SHIPPING_QUOTE_EXPIRED', 'SHIPPING_QUOTE_OUTDATED', 'SHIPPING_QUOTE_NOT_FOUND', 'SHIPPING_METHOD_UNAVAILABLE'];

export function CheckoutPaymentPage() {
  const { basket, startNewBasket } = useBasket();
  const checkout = loadCheckout();

  const [card, setCard] = useState({ number: '', cardholderName: checkout?.customer.fullName ?? '', expiryMonth: '', expiryYear: '', cvv: '' });
  const [dateOfBirth, setDateOfBirth] = useState('');
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [error, setError] = useState<unknown>(null);
  const [placing, setPlacing] = useState(false);
  const [placedOrderNumber, setPlacedOrderNumber] = useState<string | null>(null);

  const preview = useQuery({
    queryKey: ['order-preview', checkout?.basketId, checkout?.quoteId, checkout?.shippingMethod, basket?.total],
    queryFn: () => ordersApi.preview({
      basketId: checkout!.basketId,
      shippingQuoteId: checkout!.quoteId,
      shippingMethod: checkout!.shippingMethod,
    }),
    enabled: !!checkout && !placing,
    retry: false,
  });

  // Checked first: once the order is placed the checkout state is cleared, which would otherwise redirect.
  if (placedOrderNumber) return <Navigate to={`/order-confirmation/${placedOrderNumber}`} replace />;
  if (!checkout || (basket && basket.id !== checkout.basketId)) return <Navigate to="/checkout/shipping" replace />;

  const previewError = preview.error instanceof ApiError ? preview.error : null;
  const mustRestartShipping = previewError && RESTART_SHIPPING_CODES.includes(previewError.code);

  const placeOrder = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);
    setFieldErrors({});

    const localErrors: Record<string, string[]> = {};
    if (!card.number.trim()) localErrors['card.number'] = ['Card number is required.'];
    if (!card.cardholderName.trim()) localErrors['card.cardholderName'] = ['Cardholder name is required.'];
    if (!card.expiryMonth || !card.expiryYear) localErrors['card.expiryYear'] = ['Expiry date is required.'];
    if (!card.cvv.trim()) localErrors['card.cvv'] = ['Security code is required.'];
    if (preview.data?.requiresAgeVerification && !dateOfBirth) {
      localErrors['ageVerification.dateOfBirth'] = ['Please enter your date of birth.'];
    }
    if (Object.keys(localErrors).length > 0) {
      setFieldErrors(localErrors);
      return;
    }

    setPlacing(true);
    try {
      const order = await ordersApi.placeOrder({
        basketId: checkout.basketId,
        customer: checkout.customer,
        shippingQuoteId: checkout.quoteId,
        shippingMethod: checkout.shippingMethod,
        payment: {
          number: card.number,
          cardholderName: card.cardholderName,
          expiryMonth: Number(card.expiryMonth),
          expiryYear: Number(card.expiryYear),
          cvv: card.cvv,
        },
        ageVerification: preview.data?.requiresAgeVerification ? { dateOfBirth } : undefined,
      });
      saveLastOrder(order.orderNumber, order.customerEmail);
      clearCheckout();
      startNewBasket();
      setPlacedOrderNumber(order.orderNumber);
      return;
    } catch (e) {
      if (e instanceof ApiError && e.status === 400 && Object.keys(e.fieldErrors).length > 0) {
        setFieldErrors(e.fieldErrors);
        setError(new Error('Please check the highlighted fields.'));
      } else {
        setError(e);
      }
      void preview.refetch();
    } finally {
      setPlacing(false);
    }
  };

  const customerErrors = Object.entries(fieldErrors).filter(([key]) => key.startsWith('customer.'));

  return (
    <div className="checkout">
      <CheckoutSteps current={2} />
      <h1>Payment</h1>

      <div className="checkout-layout">
        <form className="checkout-form" onSubmit={placeOrder} noValidate data-testid="payment-form">
          <fieldset disabled={placing}>
            <legend>Card details</legend>
            <Field id="cardNumber" label="Card number" error={fieldErrors['card.number']}>
              <input id="cardNumber" inputMode="numeric" autoComplete="cc-number" placeholder="1234 5678 9012 3456"
                value={card.number} onChange={(e) => setCard({ ...card, number: e.target.value })} />
            </Field>
            <Field id="cardholderName" label="Name on card" error={fieldErrors['card.cardholderName']}>
              <input id="cardholderName" autoComplete="cc-name" value={card.cardholderName}
                onChange={(e) => setCard({ ...card, cardholderName: e.target.value })} />
            </Field>
            <div className="field-row">
              <Field id="expiryMonth" label="Expiry month" error={fieldErrors['card.expiryMonth']}>
                <select id="expiryMonth" autoComplete="cc-exp-month" value={card.expiryMonth}
                  onChange={(e) => setCard({ ...card, expiryMonth: e.target.value })}>
                  <option value="">MM</option>
                  {MONTHS.map((m) => <option key={m} value={m}>{String(m).padStart(2, '0')}</option>)}
                </select>
              </Field>
              <Field id="expiryYear" label="Expiry year" error={fieldErrors['card.expiryYear']}>
                <select id="expiryYear" autoComplete="cc-exp-year" value={card.expiryYear}
                  onChange={(e) => setCard({ ...card, expiryYear: e.target.value })}>
                  <option value="">YYYY</option>
                  {YEARS.map((y) => <option key={y} value={y}>{y}</option>)}
                </select>
              </Field>
              <Field id="cvv" label="Security code" error={fieldErrors['card.cvv']} hint="3 digits, 4 for Amex">
                <input id="cvv" inputMode="numeric" autoComplete="cc-csc" maxLength={4} value={card.cvv}
                  onChange={(e) => setCard({ ...card, cvv: e.target.value })} />
              </Field>
            </div>
          </fieldset>

          {preview.data?.requiresAgeVerification && (
            <fieldset disabled={placing} data-testid="age-verification">
              <legend>Age verification</legend>
              <p className="muted">
                Your basket contains games rated for ages {preview.data.minimumAge}+. Please confirm your date of birth.
              </p>
              <Field id="dateOfBirth" label="Date of birth" error={fieldErrors['ageVerification.dateOfBirth']}>
                <input id="dateOfBirth" type="date" value={dateOfBirth} onChange={(e) => setDateOfBirth(e.target.value)} />
              </Field>
            </fieldset>
          )}

          {customerErrors.length > 0 && (
            <Notice kind="warning" testId="customer-errors">
              {customerErrors.map(([, messages]) => messages.join(' ')).join(' ')}{' '}
              <Link to="/checkout/shipping">Edit your details</Link>
            </Notice>
          )}

          <ErrorMessage error={error} testId="payment-error" />
          {placing && <Spinner label="Processing your payment..." />}

          <div className="button-row">
            <Link to="/checkout/shipping" className="button button-secondary" data-testid="back-to-shipping">Back</Link>
            <button type="submit" className="button button-primary" disabled={placing || !preview.data} data-testid="place-order">
              {preview.data ? `Place order - ${formatPrice(preview.data.total)}` : 'Place order'}
            </button>
          </div>
        </form>

        <aside className="summary-card" data-testid="order-summary">
          <h2>Order summary</h2>
          {preview.isLoading && <Spinner label="Calculating totals..." />}
          {previewError && (
            <div>
              <ErrorMessage error={previewError} testId="preview-error" />
              {mustRestartShipping && <Link to="/checkout/shipping" data-testid="restart-shipping">Calculate shipping again</Link>}
              {previewError.code === 'BASKET_HAS_ISSUES' && <Link to="/basket">Review your basket</Link>}
            </div>
          )}
          {basket && (
            <ul className="summary-lines">
              {basket.items.map((item) => (
                <li key={item.gameId} data-testid="summary-line">
                  <span>{item.quantity} x {item.title}</span>
                  <span>{formatPrice(item.lineTotal)}</span>
                </li>
              ))}
            </ul>
          )}
          {preview.data && (
            <dl className="totals">
              <dt>Subtotal</dt>
              <dd data-testid="summary-subtotal">{formatPrice(preview.data.subtotal)}</dd>
              {preview.data.discountTotal > 0 && (
                <>
                  <dt>{preview.data.discountDescription}</dt>
                  <dd className="discount" data-testid="summary-discount">-{formatPrice(preview.data.discountTotal)}</dd>
                </>
              )}
              <dt>Shipping ({preview.data.shippingMethod})</dt>
              <dd data-testid="summary-shipping">{preview.data.shippingCost === 0 ? 'FREE' : formatPrice(preview.data.shippingCost)}</dd>
              <dt>Sales tax ({formatPercent(preview.data.taxRate)})</dt>
              <dd data-testid="summary-tax">{formatPrice(preview.data.taxAmount)}</dd>
              <dt className="total">Total</dt>
              <dd className="total" data-testid="summary-total">{formatPrice(preview.data.total)}</dd>
            </dl>
          )}
          {preview.data && (
            <p className="muted small" data-testid="summary-delivery">
              Estimated delivery: {formatDate(preview.data.estimatedDeliveryDate)}
            </p>
          )}
          <div className="summary-address" data-testid="summary-address">
            <strong>Shipping to</strong>
            <p>
              {checkout.customer.fullName}<br />
              {checkout.address.line1}<br />
              {checkout.address.line2 && <>{checkout.address.line2}<br /></>}
              {checkout.address.city}, {checkout.address.region} {checkout.address.postalCode}<br />
              {checkout.address.country === 'CA' ? 'Canada' : 'United States'}
            </p>
            <p className="muted small">{checkout.customer.email} · {formatPhone(checkout.customer.phone.replace(/\D/g, '').slice(-10))}</p>
          </div>
        </aside>
      </div>
    </div>
  );
}
