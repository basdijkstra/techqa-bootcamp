import { useState } from 'react';
import { Link, useParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { catalogApi } from '../api/api';
import { ApiError } from '../api/http';
import { ErrorMessage, GameCover, Notice, PriceTag, RatingBadge, Spinner, StockBadge } from '../components/common';
import { useBasket } from '../context/BasketContext';
import { ageRatingLabels, categoryLabel, formatDate, formatDateTime } from '../format';

export function GameDetailPage() {
  const { slug = '' } = useParams();
  const { data: game, isLoading, error } = useQuery({ queryKey: ['game', slug], queryFn: () => catalogApi.getGameBySlug(slug) });
  const { addItem } = useBasket();
  const [quantity, setQuantity] = useState(1);
  const [adding, setAdding] = useState(false);
  const [addError, setAddError] = useState<unknown>(null);
  const [added, setAdded] = useState(false);

  if (isLoading) return <Spinner />;
  if (error instanceof ApiError && error.status === 404) {
    return (
      <div className="empty" data-testid="game-not-found">
        <h1>Game not found</h1>
        <Link to="/">Back to all games</Link>
      </div>
    );
  }
  if (error || !game) return <ErrorMessage error={error} />;

  const isPreOrder = game.stockStatus === 'PreOrder';
  const canBuy = game.stockStatus !== 'OutOfStock';
  const maxQuantity = isPreOrder ? 2 : Math.min(5, game.availableQuantity);

  const add = async () => {
    setAdding(true);
    setAddError(null);
    setAdded(false);
    try {
      await addItem(game.id, quantity);
      setAdded(true);
    } catch (e) {
      setAddError(e);
    } finally {
      setAdding(false);
    }
  };

  return (
    <article className="game-detail" data-testid="game-detail">
      <div className="game-detail-cover">
        <GameCover title={game.title} primary={game.coverColorPrimary} secondary={game.coverColorSecondary} size="large" />
      </div>
      <div className="game-detail-info">
        <Link to="/" className="back-link">&larr; All games</Link>
        <h1 data-testid="game-title">{game.title}</h1>
        <p className="muted">
          {game.developer} · Published by {game.publisher}
        </p>
        <dl className="facts">
          <dt>Category</dt>
          <dd data-testid="game-category">{categoryLabel(game.category)}</dd>
          <dt>Platforms</dt>
          <dd data-testid="game-platforms">{game.platforms.join(', ')}</dd>
          <dt>Rating</dt>
          <dd><RatingBadge rating={game.ageRating} /> {ageRatingLabels[game.ageRating]}</dd>
          <dt>Release date</dt>
          <dd data-testid="release-date">{formatDate(game.releaseDate)}</dd>
        </dl>
        <p className="description" data-testid="game-description">{game.description}</p>

        <div className="buy-box" data-testid="buy-box">
          <PriceTag pricing={game.pricing} />
          {game.pricing.isOnSale && game.pricing.saleEndsAt && (
            <p className="muted" data-testid="sale-ends">Sale ends {formatDateTime(game.pricing.saleEndsAt)}</p>
          )}
          <StockBadge status={game.stockStatus} quantity={game.availableQuantity} />
          {isPreOrder && (
            <Notice testId="preorder-notice">
              Pre-order now: ships on release day, {formatDate(game.releaseDate)}. Limit 2 per customer.
            </Notice>
          )}
          {game.minimumAge >= 17 && (
            <Notice kind="warning" testId="age-notice">
              Rated {game.ageRating}. You must be {game.minimumAge} or older to buy this game.
            </Notice>
          )}

          {canBuy ? (
            <div className="add-to-basket">
              <label htmlFor="quantity">Quantity</label>
              <select id="quantity" value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} data-testid="quantity-select">
                {Array.from({ length: Math.max(maxQuantity, 1) }, (_, i) => i + 1).map((n) => (
                  <option key={n} value={n}>{n}</option>
                ))}
              </select>
              <button type="button" className="button button-primary" onClick={add} disabled={adding} data-testid="add-to-basket">
                {adding ? 'Adding...' : isPreOrder ? 'Pre-order' : 'Add to basket'}
              </button>
            </div>
          ) : (
            <p className="muted" data-testid="out-of-stock-message">This game is currently out of stock.</p>
          )}

          {added && (
            <Notice kind="success" testId="added-to-basket">
              Added to your basket. <Link to="/basket" data-testid="go-to-basket">View basket</Link>
            </Notice>
          )}
          <ErrorMessage error={addError} testId="add-to-basket-error" />
        </div>
      </div>
    </article>
  );
}
