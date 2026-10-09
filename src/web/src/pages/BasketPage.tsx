import { useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router';
import type { BasketLine } from '../api/types';
import { ErrorMessage, GameCover, Notice, RatingBadge, Spinner } from '../components/common';
import { useBasket } from '../context/BasketContext';
import { formatDate, formatPrice } from '../format';

export function BasketPage() {
  const { basket, isLoading, updateItem, removeItem, applyPromoCode, removePromoCode } = useBasket();
  const navigate = useNavigate();
  const [error, setError] = useState<unknown>(null);
  const [promoInput, setPromoInput] = useState('');
  const [promoError, setPromoError] = useState<unknown>(null);
  const [busy, setBusy] = useState(false);

  if (isLoading || !basket) return <Spinner label="Loading basket..." />;

  const run = async (action: () => Promise<unknown>, onError: (e: unknown) => void = setError) => {
    setBusy(true);
    setError(null);
    setPromoError(null);
    try {
      await action();
    } catch (e) {
      onError(e);
    } finally {
      setBusy(false);
    }
  };

  const applyPromo = (event: FormEvent) => {
    event.preventDefault();
    void run(async () => {
      await applyPromoCode(promoInput);
      setPromoInput('');
    }, setPromoError);
  };

  if (basket.items.length === 0) {
    return (
      <div className="empty" data-testid="empty-basket">
        <h1>Your basket is empty</h1>
        <p>Find something to play in the <Link to="/">catalog</Link>.</p>
      </div>
    );
  }

  return (
    <div className="basket-page">
      <h1>Your basket</h1>
      {basket.messages.map((message) => (
        <Notice key={message} kind="warning" testId="basket-message">{message}</Notice>
      ))}
      <ErrorMessage error={error} testId="basket-error" />

      <div className="basket-layout">
        <ul className="basket-lines" data-testid="basket-lines">
          {basket.items.map((line) => (
            <BasketLineRow key={line.gameId} line={line} busy={busy}
              onQuantityChange={(q) => run(() => updateItem(line.gameId, q))}
              onRemove={() => run(() => removeItem(line.gameId))} />
          ))}
        </ul>

        <aside className="summary-card" data-testid="basket-summary">
          <h2>Summary</h2>
          <dl className="totals">
            <dt>Subtotal ({basket.itemCount} {basket.itemCount === 1 ? 'item' : 'items'})</dt>
            <dd data-testid="basket-subtotal">{formatPrice(basket.subtotal)}</dd>
            {basket.discountTotal > 0 && (
              <>
                <dt data-testid="discount-description">{basket.discountDescription}</dt>
                <dd className="discount" data-testid="basket-discount">-{formatPrice(basket.discountTotal)}</dd>
              </>
            )}
            <dt className="total">Total</dt>
            <dd className="total" data-testid="basket-total">{formatPrice(basket.total)}</dd>
          </dl>
          <p className="muted small">Shipping and sales tax are calculated at checkout.</p>

          {basket.promoCode ? (
            <div className="promo-applied" data-testid="promo-code-status" data-applied={basket.promoCode.isApplied}>
              <div>
                <strong data-testid="promo-code">{basket.promoCode.code}</strong>
                <p className={basket.promoCode.isValid ? 'muted small' : 'field-error'} data-testid="promo-code-message">
                  {basket.promoCode.message}
                </p>
              </div>
              <button type="button" className="link-button" onClick={() => run(removePromoCode)} disabled={busy}
                data-testid="remove-promo-code">
                Remove
              </button>
            </div>
          ) : (
            <form className="promo-form" onSubmit={applyPromo}>
              <label htmlFor="promo-code">Promo code</label>
              <div className="input-row">
                <input id="promo-code" value={promoInput} onChange={(e) => setPromoInput(e.target.value)} autoComplete="off"
                  data-testid="promo-code-input" />
                <button type="submit" className="button button-small" disabled={busy || !promoInput.trim()} data-testid="apply-promo-code">
                  Apply
                </button>
              </div>
              <ErrorMessage error={promoError} testId="promo-code-error" />
            </form>
          )}

          <p className="muted small">Bundle deal: buy 3 games in the same category and get the cheapest one 50% off. Can't be combined with promo codes.</p>

          <button type="button" className="button button-primary button-block" disabled={!basket.canCheckout || busy}
            onClick={() => navigate('/checkout/shipping')} data-testid="checkout-button">
            Checkout
          </button>
        </aside>
      </div>
    </div>
  );
}

function BasketLineRow({ line, busy, onQuantityChange, onRemove }: {
  line: BasketLine;
  busy: boolean;
  onQuantityChange: (quantity: number) => void;
  onRemove: () => void;
}) {
  const max = Math.max(line.isPreOrder ? 2 : 5, line.quantity);
  return (
    <li className="basket-line" data-testid="basket-line" data-game-id={line.gameId}>
      <GameCover title={line.title} primary={line.coverColorPrimary} secondary={line.coverColorSecondary} size="small" />
      <div className="basket-line-info">
        <h2>
          {line.slug ? <Link to={`/games/${line.slug}`} data-testid="line-title">{line.title}</Link> : <span data-testid="line-title">{line.title}</span>}
        </h2>
        <div className="muted small">
          <RatingBadge rating={line.ageRating} />{' '}
          {line.isPreOrder && line.releaseDate && <span data-testid="line-preorder">Pre-order, ships {formatDate(line.releaseDate)}</span>}
        </div>
        <div className="unit-price" data-testid="line-unit-price">
          {formatPrice(line.unitPrice)} each
          {line.priceChanged && (
            <span className="price-changed" data-testid="line-price-changed"> (was {formatPrice(line.unitPriceWhenAdded)})</span>
          )}
        </div>
        {line.issues.map((issue) => <p key={issue} className="field-error" data-testid="line-issue">{issue}</p>)}
      </div>
      <div className="basket-line-actions">
        <label htmlFor={`quantity-${line.gameId}`} className="visually-hidden">Quantity for {line.title}</label>
        <select id={`quantity-${line.gameId}`} value={line.quantity} disabled={busy}
          onChange={(e) => onQuantityChange(Number(e.target.value))} data-testid="line-quantity">
          {Array.from({ length: max }, (_, i) => i + 1).map((n) => <option key={n} value={n}>{n}</option>)}
        </select>
        <strong data-testid="line-total">{formatPrice(line.lineTotal)}</strong>
        <button type="button" className="link-button" onClick={onRemove} disabled={busy} data-testid="remove-line">
          Remove
        </button>
      </div>
    </li>
  );
}
