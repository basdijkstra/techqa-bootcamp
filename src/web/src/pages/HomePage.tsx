import { useEffect, useState, type FormEvent } from 'react';
import { Link, useSearchParams } from 'react-router';
import { useQuery } from '@tanstack/react-query';
import { catalogApi } from '../api/api';
import type { GameSummary } from '../api/types';
import { ErrorMessage, GameCover, PriceTag, RatingBadge, Spinner, StockBadge } from '../components/common';
import { categoryLabel } from '../format';

const PRICE_BUCKETS = [
  { label: 'Under $15', min: '', max: '14.99' },
  { label: '$15 - $30', min: '15', max: '30' },
  { label: '$30 - $50', min: '30', max: '50' },
  { label: 'Over $50', min: '50', max: '' },
];

const SORT_OPTIONS = [
  { value: 'featured', label: 'Featured' },
  { value: 'price-asc', label: 'Price: low to high' },
  { value: 'price-desc', label: 'Price: high to low' },
  { value: 'title', label: 'Title A-Z' },
  { value: 'newest', label: 'Newest first' },
];

/** The catalog. Every filter is kept in the URL, so a filtered view can be bookmarked or opened directly. */
export function HomePage() {
  const [params, setParams] = useSearchParams();
  const categories = params.getAll('category');
  const query = {
    category: categories,
    platform: params.get('platform') ?? undefined,
    minPrice: params.get('minPrice') ?? undefined,
    maxPrice: params.get('maxPrice') ?? undefined,
    onSale: params.get('onSale') === 'true',
    inStockOnly: params.get('inStockOnly') === 'true',
    search: params.get('search') ?? undefined,
    sort: params.get('sort') ?? 'featured',
    page: Number(params.get('page') ?? 1),
    pageSize: 12,
  };

  const games = useQuery({ queryKey: ['games', query], queryFn: () => catalogApi.searchGames(query) });
  const categoryList = useQuery({ queryKey: ['categories'], queryFn: catalogApi.getCategories });
  const platforms = useQuery({ queryKey: ['platforms'], queryFn: catalogApi.getPlatforms });

  const [minPrice, setMinPrice] = useState(query.minPrice ?? '');
  const [maxPrice, setMaxPrice] = useState(query.maxPrice ?? '');
  const [search, setSearch] = useState(query.search ?? '');
  useEffect(() => {
    setMinPrice(params.get('minPrice') ?? '');
    setMaxPrice(params.get('maxPrice') ?? '');
    setSearch(params.get('search') ?? '');
  }, [params]);

  /** Updates URL parameters. Any change except paging goes back to page 1. */
  const update = (changes: Record<string, string | string[] | null>) => {
    const next = new URLSearchParams(params);
    for (const [key, value] of Object.entries(changes)) {
      next.delete(key);
      if (Array.isArray(value)) value.forEach((v) => next.append(key, v));
      else if (value) next.set(key, value);
    }
    if (!('page' in changes)) next.delete('page');
    setParams(next);
  };

  const toggleCategory = (name: string) =>
    update({ category: categories.includes(name) ? categories.filter((c) => c !== name) : [...categories, name] });

  const applyPrice = (event: FormEvent) => {
    event.preventDefault();
    update({ minPrice: minPrice.trim() || null, maxPrice: maxPrice.trim() || null });
  };

  const applySearch = (event: FormEvent) => {
    event.preventDefault();
    update({ search: search.trim() || null });
  };

  const hasFilters = [...params.keys()].some((k) => k !== 'sort' && k !== 'page');

  return (
    <div className="catalog">
      <aside className="filters" aria-label="Filters" data-testid="filters">
        <form onSubmit={applySearch} className="filter-group" role="search">
          <label htmlFor="search" className="filter-title">Search</label>
          <div className="input-row">
            <input id="search" type="search" placeholder="Title or publisher" value={search}
              onChange={(e) => setSearch(e.target.value)} data-testid="search-input" />
            <button type="submit" className="button button-small" data-testid="search-button">Go</button>
          </div>
        </form>

        <fieldset className="filter-group" data-testid="category-filter">
          <legend className="filter-title">Category</legend>
          {categoryList.data?.map((c) => (
            <label key={c.name} className="checkbox">
              <input type="checkbox" checked={categories.includes(c.name)} onChange={() => toggleCategory(c.name)}
                data-testid={`category-${c.name}`} />
              {c.displayName} <span className="muted">({c.gameCount})</span>
            </label>
          ))}
        </fieldset>

        <div className="filter-group">
          <label htmlFor="platform" className="filter-title">Platform</label>
          <select id="platform" value={query.platform ?? ''} onChange={(e) => update({ platform: e.target.value || null })}
            data-testid="platform-filter">
            <option value="">All platforms</option>
            {platforms.data?.map((p) => <option key={p} value={p}>{p}</option>)}
          </select>
        </div>

        <form className="filter-group" onSubmit={applyPrice} data-testid="price-filter">
          <span className="filter-title">Price</span>
          <div className="price-buckets">
            {PRICE_BUCKETS.map((bucket) => {
              const active = (query.minPrice ?? '') === bucket.min && (query.maxPrice ?? '') === bucket.max;
              return (
                <button key={bucket.label} type="button" className={`chip ${active ? 'chip-active' : ''}`} aria-pressed={active}
                  onClick={() => update({ minPrice: bucket.min || null, maxPrice: bucket.max || null })}>
                  {bucket.label}
                </button>
              );
            })}
          </div>
          <div className="input-row">
            <label className="visually-hidden" htmlFor="min-price">Minimum price</label>
            <input id="min-price" type="number" min="0" step="0.01" placeholder="Min $" value={minPrice}
              onChange={(e) => setMinPrice(e.target.value)} data-testid="min-price" />
            <span>-</span>
            <label className="visually-hidden" htmlFor="max-price">Maximum price</label>
            <input id="max-price" type="number" min="0" step="0.01" placeholder="Max $" value={maxPrice}
              onChange={(e) => setMaxPrice(e.target.value)} data-testid="max-price" />
            <button type="submit" className="button button-small" data-testid="apply-price">Apply</button>
          </div>
        </form>

        <div className="filter-group">
          <label className="checkbox">
            <input type="checkbox" checked={query.onSale} onChange={(e) => update({ onSale: e.target.checked ? 'true' : null })}
              data-testid="on-sale-filter" />
            On sale
          </label>
          <label className="checkbox">
            <input type="checkbox" checked={query.inStockOnly}
              onChange={(e) => update({ inStockOnly: e.target.checked ? 'true' : null })} data-testid="in-stock-filter" />
            In stock (ships now)
          </label>
        </div>

        {hasFilters && (
          <button type="button" className="button button-secondary" onClick={() => setParams(new URLSearchParams())}
            data-testid="clear-filters">
            Clear all filters
          </button>
        )}
      </aside>

      <section className="results" aria-label="Games">
        <div className="results-header">
          <h1>{categories.length === 1 ? `${categoryLabel(categories[0])} games` : 'All games'}</h1>
          <div className="results-tools">
            {games.data && (
              <span className="muted" data-testid="result-count">
                {games.data.totalCount} {games.data.totalCount === 1 ? 'game' : 'games'}
              </span>
            )}
            <label htmlFor="sort" className="visually-hidden">Sort by</label>
            <select id="sort" value={query.sort} onChange={(e) => update({ sort: e.target.value })} data-testid="sort">
              {SORT_OPTIONS.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
            </select>
          </div>
        </div>

        {games.isLoading && <Spinner label="Loading games..." />}
        <ErrorMessage error={games.error} />

        {games.data && games.data.items.length === 0 && (
          <p className="empty" data-testid="no-results">No games match your filters.</p>
        )}

        <ul className="game-grid" data-testid="game-grid">
          {games.data?.items.map((game) => <GameCard key={game.id} game={game} />)}
        </ul>

        {games.data && games.data.totalPages > 1 && (
          <nav className="pagination" aria-label="Pagination" data-testid="pagination">
            <button type="button" className="button button-secondary" disabled={query.page <= 1}
              onClick={() => update({ page: String(query.page - 1) })} data-testid="previous-page">
              Previous
            </button>
            <span data-testid="page-indicator">Page {games.data.page} of {games.data.totalPages}</span>
            <button type="button" className="button button-secondary" disabled={query.page >= games.data.totalPages}
              onClick={() => update({ page: String(query.page + 1) })} data-testid="next-page">
              Next
            </button>
          </nav>
        )}
      </section>
    </div>
  );
}

function GameCard({ game }: { game: GameSummary }) {
  return (
    <li className="game-card" data-testid="game-card" data-game-id={game.id}>
      <Link to={`/games/${game.slug}`} className="game-card-link" aria-label={game.title}>
        <GameCover title={game.title} primary={game.coverColorPrimary} secondary={game.coverColorSecondary} />
      </Link>
      <div className="game-card-body">
        <div className="game-card-meta">
          <span className="muted">{categoryLabel(game.category)}</span>
          <RatingBadge rating={game.ageRating} />
        </div>
        <h2 className="game-card-title">
          <Link to={`/games/${game.slug}`} data-testid="game-title">{game.title}</Link>
        </h2>
        <div className="platforms" data-testid="platforms">{game.platforms.join(' · ')}</div>
        <div className="game-card-footer">
          <PriceTag pricing={game.pricing} />
          <StockBadge status={game.stockStatus} quantity={game.availableQuantity} />
        </div>
      </div>
    </li>
  );
}
