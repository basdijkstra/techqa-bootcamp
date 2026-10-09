import { useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../../api/api';
import { ErrorMessage, OrderStatusBadge, Spinner } from '../../components/common';
import { formatDateTime, formatPrice, orderStatusLabels } from '../../format';

export function OrdersPage() {
  const [params, setParams] = useSearchParams();
  const query = {
    status: params.get('status') ?? undefined,
    from: params.get('from') ?? undefined,
    to: params.get('to') ?? undefined,
    search: params.get('search') ?? undefined,
    page: Number(params.get('page') ?? 1),
    pageSize: 20,
  };
  const orders = useQuery({ queryKey: ['admin-orders', query], queryFn: () => ordersApi.adminList(query) });
  const [search, setSearch] = useState(query.search ?? '');

  const update = (changes: Record<string, string | null>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(changes)) {
      if (value) next.set(key, value);
      else next.delete(key);
    }
    if (!('page' in changes)) next.delete('page');
    setParams(next);
  };

  const submitSearch = (event: FormEvent) => {
    event.preventDefault();
    update({ search: search.trim() || null });
  };

  return (
    <div className="orders-page">
      <h1>Orders</h1>
      <div className="toolbar" data-testid="order-filters">
        <form onSubmit={submitSearch} className="input-row">
          <label htmlFor="order-search" className="visually-hidden">Search orders</label>
          <input id="order-search" type="search" placeholder="Order number, name or email" value={search}
            onChange={(e) => setSearch(e.target.value)} data-testid="order-search" />
          <button type="submit" className="button button-small">Search</button>
        </form>
        <label htmlFor="status-filter" className="visually-hidden">Status</label>
        <select id="status-filter" value={query.status ?? ''} onChange={(e) => update({ status: e.target.value || null })}
          data-testid="status-filter">
          <option value="">All statuses</option>
          {Object.entries(orderStatusLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <label htmlFor="from-date">From</label>
        <input id="from-date" type="date" value={query.from ?? ''} onChange={(e) => update({ from: e.target.value || null })}
          data-testid="from-date" />
        <label htmlFor="to-date">To</label>
        <input id="to-date" type="date" value={query.to ?? ''} onChange={(e) => update({ to: e.target.value || null })}
          data-testid="to-date" />
      </div>

      {orders.isLoading && <Spinner />}
      <ErrorMessage error={orders.error} />
      {orders.data && (
        <>
          <p className="muted" data-testid="order-count">{orders.data.totalCount} orders</p>
          <table className="table" data-testid="orders-table">
            <thead>
              <tr><th>Order</th><th>Date</th><th>Customer</th><th>Ship to</th><th className="numeric">Items</th><th className="numeric">Total</th><th>Status</th></tr>
            </thead>
            <tbody>
              {orders.data.items.map((o) => (
                <tr key={o.orderNumber} data-testid="order-row" data-order-number={o.orderNumber}>
                  <td><Link to={`/admin/orders/${o.orderNumber}`} data-testid="order-link">{o.orderNumber}</Link></td>
                  <td>{formatDateTime(o.createdAt)}</td>
                  <td>{o.customerName}<div className="muted small">{o.customerEmail}</div></td>
                  <td>{o.shippingRegion}, {o.shippingCountry}</td>
                  <td className="numeric">{o.itemCount}</td>
                  <td className="numeric">{formatPrice(o.total)}</td>
                  <td><OrderStatusBadge status={o.status} /></td>
                </tr>
              ))}
            </tbody>
          </table>
          {orders.data.items.length === 0 && <p className="empty" data-testid="no-orders">No orders found.</p>}
          {orders.data.totalPages > 1 && (
            <nav className="pagination" aria-label="Pagination">
              <button type="button" className="button button-secondary" disabled={query.page <= 1}
                onClick={() => update({ page: String(query.page - 1) })}>Previous</button>
              <span>Page {orders.data.page} of {orders.data.totalPages}</span>
              <button type="button" className="button button-secondary" disabled={query.page >= orders.data.totalPages}
                onClick={() => update({ page: String(query.page + 1) })}>Next</button>
            </nav>
          )}
        </>
      )}
    </div>
  );
}
