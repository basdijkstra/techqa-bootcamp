import { useState, type FormEvent } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { catalogApi } from '../../api/api';
import { ApiError } from '../../api/http';
import type { StockItem } from '../../api/types';
import { ErrorMessage, Field, Notice, Spinner, StockBadge } from '../../components/common';
import { useAuth } from '../../context/AuthContext';
import { categoryLabel, formatDateTime, formatPrice } from '../../format';

const REASONS = [
  { value: 'Restock', label: 'Restock (adds stock)' },
  { value: 'Return', label: 'Customer return (adds stock)' },
  { value: 'Damaged', label: 'Damaged (removes stock)' },
  { value: 'Lost', label: 'Lost (removes stock)' },
  { value: 'Correction', label: 'Stock count correction (note required)' },
];

export function StockPage() {
  const { isAdmin } = useAuth();
  const stock = useQuery({ queryKey: ['admin-stock'], queryFn: catalogApi.getStock });
  const [selected, setSelected] = useState<StockItem | null>(null);
  const [filter, setFilter] = useState('');

  if (stock.isLoading) return <Spinner />;
  if (stock.error) return <ErrorMessage error={stock.error} />;

  const rows = stock.data!.filter((g) => g.title.toLowerCase().includes(filter.toLowerCase()));

  return (
    <div className="stock-page">
      <h1>Stock</h1>
      <div className="toolbar">
        <label htmlFor="stock-filter" className="visually-hidden">Filter games</label>
        <input id="stock-filter" type="search" placeholder="Filter by title" value={filter} onChange={(e) => setFilter(e.target.value)}
          data-testid="stock-filter" />
      </div>
      <div className="split">
        <table className="table" data-testid="stock-table">
          <thead>
            <tr><th>Game</th><th>Category</th><th>Price</th><th className="numeric">Stock</th><th>Status</th><th /></tr>
          </thead>
          <tbody>
            {rows.map((g) => (
              <tr key={g.gameId} data-testid="stock-row" data-game-id={g.gameId} className={selected?.gameId === g.gameId ? 'row-selected' : ''}>
                <td>{g.title}</td>
                <td>{categoryLabel(g.category)}</td>
                <td>{formatPrice(g.price)}</td>
                <td className="numeric" data-testid="stock-quantity">{g.stockQuantity}</td>
                <td><StockBadge status={g.stockStatus} /></td>
                <td>
                  <button type="button" className="link-button" onClick={() => setSelected(g)} data-testid="manage-stock">
                    {isAdmin ? 'Adjust' : 'History'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
        {selected && (
          <StockPanel key={selected.gameId} item={stock.data!.find((g) => g.gameId === selected.gameId) ?? selected}
            canEdit={isAdmin} onClose={() => setSelected(null)} />
        )}
      </div>
    </div>
  );
}

function StockPanel({ item, canEdit, onClose }: { item: StockItem; canEdit: boolean; onClose: () => void }) {
  const queryClient = useQueryClient();
  const history = useQuery({ queryKey: ['stock-history', item.gameId], queryFn: () => catalogApi.getStockHistory(item.gameId) });
  const [quantityChange, setQuantityChange] = useState('');
  const [reason, setReason] = useState('Restock');
  const [note, setNote] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [saved, setSaved] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setFieldErrors({});
    setSaved(null);
    try {
      const result = await catalogApi.adjustStock(item.gameId, {
        quantityChange: Number(quantityChange),
        reason,
        note: note.trim() || undefined,
      });
      setSaved(`Stock changed from ${result.quantityBefore} to ${result.quantityAfter}.`);
      setQuantityChange('');
      setNote('');
      await queryClient.invalidateQueries({ queryKey: ['admin-stock'] });
      await queryClient.invalidateQueries({ queryKey: ['stock-history', item.gameId] });
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) setFieldErrors(e.fieldErrors);
      else setError(e);
    } finally {
      setBusy(false);
    }
  };

  return (
    <aside className="card side-panel" data-testid="stock-panel">
      <div className="panel-header">
        <h2>{item.title}</h2>
        <button type="button" className="link-button" onClick={onClose} aria-label="Close" data-testid="close-panel">Close</button>
      </div>
      <p>Current stock: <strong data-testid="panel-stock-quantity">{item.stockQuantity}</strong></p>

      {canEdit && (
        <form onSubmit={submit} noValidate data-testid="stock-adjustment-form">
          <Field id="quantityChange" label="Quantity change" error={fieldErrors.quantityChange}
            hint="Use a negative number to remove stock, e.g. -2">
            <input id="quantityChange" type="number" step="1" value={quantityChange} onChange={(e) => setQuantityChange(e.target.value)} />
          </Field>
          <Field id="reason" label="Reason" error={fieldErrors.reason}>
            <select id="reason" value={reason} onChange={(e) => setReason(e.target.value)}>
              {REASONS.map((r) => <option key={r.value} value={r.value}>{r.label}</option>)}
            </select>
          </Field>
          <Field id="note" label="Note" error={fieldErrors.note}>
            <textarea id="note" rows={2} maxLength={250} value={note} onChange={(e) => setNote(e.target.value)} />
          </Field>
          <ErrorMessage error={error} testId="stock-error" />
          {saved && <Notice kind="success" testId="stock-saved">{saved}</Notice>}
          <button type="submit" className="button button-primary" disabled={busy || quantityChange === ''} data-testid="save-adjustment">
            Save
          </button>
        </form>
      )}

      <h3>History</h3>
      {history.isLoading && <Spinner />}
      {history.data?.length === 0 && <p className="muted" data-testid="no-history">No manual changes yet.</p>}
      <ul className="history" data-testid="stock-history">
        {history.data?.map((a) => (
          <li key={a.id} data-testid="stock-history-entry">
            <strong className={a.quantityChange > 0 ? 'positive' : 'negative'}>
              {a.quantityChange > 0 ? '+' : ''}{a.quantityChange}
            </strong>{' '}
            {a.reason} ({a.quantityBefore} &rarr; {a.quantityAfter})
            <div className="muted small">{formatDateTime(a.performedAt)} by {a.performedBy}{a.note ? ` - ${a.note}` : ''}</div>
          </li>
        ))}
      </ul>
    </aside>
  );
}
