import type { Order } from '../api/types';
import { formatDate, formatDateTime, formatPercent, formatPhone, formatPrice } from '../format';
import { OrderStatusBadge } from './common';

/** Read-only view of an order, used on the confirmation, lookup and admin pages. */
export function OrderDetails({ order }: { order: Order }) {
  const address = order.shippingAddress;
  return (
    <div className="order-details" data-testid="order-details">
      <div className="order-header">
        <div>
          <span className="muted">Order number</span>
          <h2 data-testid="order-number">{order.orderNumber}</h2>
          <span className="muted small">Placed {formatDateTime(order.createdAt)}</span>
        </div>
        <OrderStatusBadge status={order.status} />
      </div>

      <div className="order-grid">
        <section>
          <h3>Items</h3>
          <table className="table" data-testid="order-lines">
            <thead>
              <tr><th>Game</th><th>Price</th><th>Qty</th><th>Total</th></tr>
            </thead>
            <tbody>
              {order.lines.map((line) => (
                <tr key={line.gameId} data-testid="order-line">
                  <td>
                    {line.title}
                    {line.isPreOrder && line.releaseDate && <div className="muted small">Pre-order, releases {formatDate(line.releaseDate)}</div>}
                  </td>
                  <td>{formatPrice(line.unitPrice)}</td>
                  <td>{line.quantity}</td>
                  <td>{formatPrice(line.lineTotal)}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <dl className="totals">
            <dt>Subtotal</dt>
            <dd data-testid="order-subtotal">{formatPrice(order.subtotal)}</dd>
            {order.discountTotal > 0 && (
              <>
                <dt>{order.discountDescription}</dt>
                <dd className="discount" data-testid="order-discount">-{formatPrice(order.discountTotal)}</dd>
              </>
            )}
            <dt>Shipping ({order.shippingMethod})</dt>
            <dd data-testid="order-shipping">{order.shippingCost === 0 ? 'FREE' : formatPrice(order.shippingCost)}</dd>
            <dt>Sales tax ({formatPercent(order.taxRate)})</dt>
            <dd data-testid="order-tax">{formatPrice(order.taxAmount)}</dd>
            <dt className="total">Total</dt>
            <dd className="total" data-testid="order-total">{formatPrice(order.total)}</dd>
          </dl>
        </section>

        <section>
          <h3>Delivery</h3>
          <p data-testid="order-address">
            {order.customerName}<br />
            {address.line1}<br />
            {address.line2 && <>{address.line2}<br /></>}
            {address.city}, {address.region} {address.postalCode}<br />
            {address.country === 'CA' ? 'Canada' : 'United States'}
          </p>
          <p className="muted small">{order.customerEmail} · {formatPhone(order.customerPhone)}</p>
          {order.estimatedDeliveryDate && !['Cancelled', 'Refunded', 'PaymentFailed', 'Delivered'].includes(order.status) && (
            <p data-testid="order-delivery-estimate">Estimated delivery: <strong>{formatDate(order.estimatedDeliveryDate)}</strong></p>
          )}
          {order.trackingNumber && <p data-testid="order-tracking">Tracking number: <code>{order.trackingNumber}</code></p>}
          {order.cardBrand && (
            <p className="muted small" data-testid="order-payment">Paid with {order.cardBrand} ending in {order.cardLast4}</p>
          )}
          {order.cancellationReason && <p className="muted small">Cancellation reason: {order.cancellationReason}</p>}
        </section>
      </div>
    </div>
  );
}
