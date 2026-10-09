import { Link } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { catalogApi, ordersApi } from '../../api/api';
import { ErrorMessage, Spinner } from '../../components/common';
import { formatPrice, orderStatusLabels } from '../../format';

export function DashboardPage() {
  const summary = useQuery({ queryKey: ['admin-summary'], queryFn: ordersApi.adminSummary });
  const stock = useQuery({ queryKey: ['admin-stock'], queryFn: catalogApi.getStock });

  if (summary.isLoading || stock.isLoading) return <Spinner />;
  if (summary.error || stock.error) return <ErrorMessage error={summary.error ?? stock.error} />;

  const s = summary.data!;
  const lowStock = stock.data!.filter((g) => g.stockStatus === 'LowStock' || g.stockStatus === 'OutOfStock');

  return (
    <div className="dashboard">
      <h1>Dashboard</h1>
      <div className="stat-grid">
        <Stat label="Orders today" value={s.ordersToday} testId="stat-orders-today" />
        <Stat label="Awaiting shipment" value={s.awaitingShipment} testId="stat-awaiting-shipment"
          link={`/admin/orders?status=Paid`} />
        <Stat label="Revenue, last 7 days" value={formatPrice(s.revenueLast7Days)} testId="stat-revenue-7-days" />
        <Stat label="Revenue, all time" value={formatPrice(s.revenueAllTime)} testId="stat-revenue-all-time" />
      </div>

      <div className="dashboard-columns">
        <section className="card">
          <h2>Orders by status</h2>
          <table className="table" data-testid="orders-by-status">
            <tbody>
              {Object.entries(s.countByStatus).map(([status, count]) => (
                <tr key={status}>
                  <td><Link to={`/admin/orders?status=${status}`}>{orderStatusLabels[status]}</Link></td>
                  <td className="numeric" data-testid={`status-count-${status}`}>{count}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
        <section className="card">
          <h2>Low and out of stock</h2>
          {lowStock.length === 0 ? <p className="muted">All games are well stocked.</p> : (
            <table className="table" data-testid="low-stock-table">
              <tbody>
                {lowStock.map((g) => (
                  <tr key={g.gameId}>
                    <td>{g.title}</td>
                    <td className="numeric">{g.stockQuantity}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <Link to="/admin/stock">Manage stock</Link>
        </section>
      </div>
    </div>
  );
}

function Stat({ label, value, testId, link }: { label: string; value: string | number; testId: string; link?: string }) {
  const content = (
    <>
      <span className="stat-value" data-testid={testId}>{value}</span>
      <span className="stat-label">{label}</span>
    </>
  );
  return link ? <Link to={link} className="stat card">{content}</Link> : <div className="stat card">{content}</div>;
}
