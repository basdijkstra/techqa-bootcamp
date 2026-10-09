import { useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ordersApi } from '../api/api';
import { ApiError } from '../api/http';
import { CheckoutSteps } from '../components/CheckoutSteps';
import { ErrorMessage, Field, Notice, Spinner } from '../components/common';
import { OrderDetails } from '../components/OrderDetails';
import { loadLastOrder } from '../context/checkoutStorage';
import { formatDateTime } from '../format';

/** Shown right after checkout. */
export function OrderConfirmationPage() {
  const { orderNumber = '' } = useParams();
  const lastOrder = loadLastOrder();
  const email = lastOrder?.orderNumber === orderNumber ? lastOrder.email : null;

  if (!email) {
    return (
      <div className="empty">
        <h1>Order {orderNumber}</h1>
        <p>Look up your order with the email address you used. <Link to={`/orders/lookup?orderNumber=${orderNumber}`}>Find order</Link></p>
      </div>
    );
  }

  return (
    <div className="confirmation" data-testid="order-confirmation">
      <CheckoutSteps current={3} />
      <h1>Thank you for your order!</h1>
      <p>We sent a confirmation to <strong data-testid="confirmation-email">{email}</strong>.</p>
      <OrderView orderNumber={orderNumber} email={email} />
      <Link to="/" className="button button-secondary" data-testid="continue-shopping">Continue shopping</Link>
    </div>
  );
}

/** Customers look up an order with its number and their email address. */
export function OrderLookupPage() {
  const [params, setParams] = useSearchParams();
  const orderNumber = params.get('orderNumber') ?? '';
  const email = params.get('email') ?? '';
  const [form, setForm] = useState({ orderNumber, email });
  const navigate = useNavigate();

  const submit = (event: FormEvent) => {
    event.preventDefault();
    setParams({ orderNumber: form.orderNumber.trim(), email: form.email.trim() });
  };

  return (
    <div className="lookup">
      <h1>Track your order</h1>
      <form className="checkout-form inline-form" onSubmit={submit} data-testid="order-lookup-form">
        <Field id="lookupOrderNumber" label="Order number">
          <input id="lookupOrderNumber" placeholder="PV-20261009-ABCDE" value={form.orderNumber}
            onChange={(e) => setForm({ ...form, orderNumber: e.target.value })} required />
        </Field>
        <Field id="lookupEmail" label="Email address">
          <input id="lookupEmail" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} required />
        </Field>
        <button type="submit" className="button button-primary" data-testid="find-order">Find order</button>
      </form>
      {orderNumber && email && <OrderView key={`${orderNumber}-${email}`} orderNumber={orderNumber} email={email} />}
      {!orderNumber && <button type="button" className="link-button" onClick={() => navigate('/')}>Back to the shop</button>}
    </div>
  );
}

function OrderView({ orderNumber, email }: { orderNumber: string; email: string }) {
  const queryClient = useQueryClient();
  const queryKey = ['order', orderNumber, email];
  const { data: order, isLoading, error } = useQuery({ queryKey, queryFn: () => ordersApi.getOrder(orderNumber, email), retry: false });
  const [confirming, setConfirming] = useState(false);
  const [cancelling, setCancelling] = useState(false);
  const [cancelError, setCancelError] = useState<unknown>(null);

  if (isLoading) return <Spinner label="Loading order..." />;
  if (error instanceof ApiError && error.status === 404) {
    return <Notice kind="warning" testId="order-not-found">We could not find an order with that number and email address.</Notice>;
  }
  if (error || !order) return <ErrorMessage error={error} />;

  const cancel = async () => {
    setCancelling(true);
    setCancelError(null);
    try {
      const updated = await ordersApi.cancelOrder(orderNumber, email);
      queryClient.setQueryData(queryKey, updated);
      setConfirming(false);
    } catch (e) {
      setCancelError(e);
    } finally {
      setCancelling(false);
    }
  };

  return (
    <div>
      <OrderDetails order={order} />
      {order.status === 'Cancelled' && <Notice kind="success" testId="order-cancelled">This order was cancelled and your payment refunded.</Notice>}
      {order.canBeCancelledByCustomer && (
        <div className="cancel-box" data-testid="cancel-box">
          <p className="muted small" data-testid="cancellation-deadline">
            You can cancel this order until {formatDateTime(order.cancellationDeadline)}.
          </p>
          {confirming ? (
            <div className="button-row">
              <span>Cancel this order and refund {order.cardBrand ? `your ${order.cardBrand}` : 'your card'}?</span>
              <button type="button" className="button button-danger" onClick={cancel} disabled={cancelling} data-testid="confirm-cancel">
                {cancelling ? 'Cancelling...' : 'Yes, cancel order'}
              </button>
              <button type="button" className="button button-secondary" onClick={() => setConfirming(false)} data-testid="keep-order">
                Keep order
              </button>
            </div>
          ) : (
            <button type="button" className="button button-secondary" onClick={() => setConfirming(true)} data-testid="cancel-order">
              Cancel order
            </button>
          )}
        </div>
      )}
      <ErrorMessage error={cancelError} testId="cancel-error" />
    </div>
  );
}
