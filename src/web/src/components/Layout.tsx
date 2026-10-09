import { Link, NavLink, Outlet } from 'react-router';
import { useBasket } from '../context/BasketContext';
import { useAuth } from '../context/AuthContext';

export function Layout() {
  const { basket } = useBasket();
  const { user } = useAuth();

  return (
    <div className="app">
      <header className="site-header">
        <div className="container header-inner">
          <Link to="/" className="logo" data-testid="logo">
            <span className="logo-mark">PV</span> PixelVault
          </Link>
          <nav className="main-nav" aria-label="Main">
            <NavLink to="/" end>Games</NavLink>
            <NavLink to="/orders/lookup" data-testid="nav-order-lookup">Track order</NavLink>
            {user ? (
              <NavLink to="/admin" data-testid="nav-admin">Admin</NavLink>
            ) : (
              <NavLink to="/admin/login" data-testid="nav-admin-login">Staff login</NavLink>
            )}
            <NavLink to="/basket" className="basket-link" data-testid="nav-basket">
              Basket
              <span className="basket-count" data-testid="basket-count" aria-label={`${basket?.itemCount ?? 0} items in basket`}>
                {basket?.itemCount ?? 0}
              </span>
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="container main">
        <Outlet />
      </main>
      <footer className="site-footer">
        <div className="container">
          PixelVault is a fictional store built for testing practice. No real orders are placed and no money is taken.
        </div>
      </footer>
    </div>
  );
}
