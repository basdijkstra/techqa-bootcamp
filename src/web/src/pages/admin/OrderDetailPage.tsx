import { useState, type FormEvent } from 'react';
import { Link, useParams } from 'react-router';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ordersApi } from '../../api/api';
import { ApiError } from '../../api/http';
import type { OrderStatus } from '../../api/types';
import { ErrorMessage, Field, Notice, Spinner } from '../../components/common';
import { OrderDetails } from '../../components/OrderDetails';
import { useAuth } from '../../context/AuthContext';
import { formatDateTime, orderStatusLabels } from '../../format';

const ACTION_LABELS: Partial<Record<OrderStatus, string>> = {
  Shipped: 'Mark as shipped',
  Delivered: 'Mark as delivered',
  Cancelled: 'Cancel order',
  Refunded: 'Refund order',
};

export function OrderDetailPage() {
  const { orderNumber = '' } = useParams();
  const { isAdmin } = useAuth();
  const queryClient = useQueryClient();
  const order = useQuery({ queryKey: ['admin-order', orderNumber], queryFn: () => ordersApi.adminGet(orderNumber) });
  const [action, setAction] = useState<OrderStatus | null>(null);
  const [trackingNumber, setTrackingNumber] = useState('');
  const [note, setNote] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState<string | null>(null);

  if (order.isLoading) return <Spinner />;
  if (order.error instanceof ApiError && order.error.status === 404) {
    return <Notice kind="warning" testId="order-not-found">Order {orderNumber} was not found.</Notice>;
  }
  if (order.error || !order.data) return <ErrorMessage error={order.error} />;

  const data = order.data;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (!action) return;
    setBusy(true);
    setError(null);
    setFieldErrors({});
    try {
      const updated = await ordersApi.adminChangeStatus(orderNumber, {
        status: action,
        trackingNumber: action === 'Shipped' ? trackingNumber : undefined,
        note: note.trim() || undefined,
      });
      queryClient.setQueryData(['admin-order', orderNumber], updated);
      void queryClient.invalidateQueries({ queryKey: ['admin-orders'] });
      void queryClient.invalidateQueries({ queryKey: ['admin-summary'] });
      setDone(`Order is now ${orderStatusLabels[updated.order.status]}.`);
      setAction(null);
      setTrackingNumber('');
      setNote('');
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) setFieldErrors(e.fieldErrors);
      else setError(e);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="admin-order">
      <Link to="/admin/orders" className="back-link">&larr; All orders</Link>
      <OrderDetails order={data.order} />
      {data.paymentFailureCode && (
        <p className="muted" data-testid="payment-failure">Payment failure: <code>{data.paymentFailureCode}</code></p>
      )}

      {done && <Notice kind="success" testId="status-changed">{done}</Notice>}

      {isAdmin && data.allowedStatusChanges.length > 0 && (
        <section className="card" data-testid="status-actions">
          <h2>Actions</h2>
          <div className="button-row">
            {data.allowedStatusChanges.map((status) => (
              <button key={status} type="button"
                className={`button ${status === 'Cancelled' || status === 'Refunded' ? 'button-danger' : 'button-primary'}`}
                onClick={() => { setAction(status); setDone(null); setError(null); setFieldErrors({}); }}
                data-testid={`action-${status}`}>
                {ACTION_LABELS[status] ?? status}
              </button>
            ))}
          </div>
          {action && (
            <form onSubmit={submit} noValidate className="action-form" data-testid="status-form">
              <h3>{ACTION_LABELS[action]}</h3>
              {action === 'Shipped' && (
                <Field id="trackingNumber" label="Tracking number" error={fieldErrors.trackingNumber}>
                  <input id="trackingNumber" value={trackingNumber} onChange={(e) => setTrackingNumber(e.target.value)} />
                </Field>
              )}
              <Field id="statusNote" label={action === 'Cancelled' || action === 'Refunded' ? 'Reason' : 'Note (optional)'}
                error={fieldErrors.note}>
                <textarea id="statusNote" rows={2} value={note} onChange={(e) => setNote(e.target.value)} />
              </Field>
              <ErrorMessage error={error} testId="status-error" />
              <div className="button-row">
                <button type="button" className="button button-secondary" onClick={() => setAction(null)}>Back</button>
                <button type="submit" className="button button-primary" disabled={busy} data-testid="confirm-status-change">
                  {busy ? 'Saving...' : 'Confirm'}
                </button>
              </div>
            </form>
          )}
        </section>
      )}

      <section className="card">
        <h2>History</h2>
        <ol className="history" data-testid="order-history">
          {data.history.map((h, i) => (
            <li key={i} data-testid="order-history-entry">
              <strong>{orderStatusLabels[h.toStatus]}</strong>
              <div className="muted small">{formatDateTime(h.changedAt)} by {h.changedBy}{h.note ? ` - ${h.note}` : ''}</div>
            </li>
          ))}
        </ol>
      </section>
    </div>
  );
}
