import { useState, type FormEvent } from 'react';
import { Navigate, NavLink, Outlet, useLocation, useNavigate } from 'react-router';
import { ApiError } from '../../api/http';
import { ErrorMessage, Field } from '../../components/common';
import { useAuth } from '../../context/AuthContext';

/** Admin pages need a signed in staff member. Others are sent to the login page. */
export function AdminLayout() {
  const { user, logout } = useAuth();
  const location = useLocation();

  if (!user) return <Navigate to="/admin/login" replace state={{ from: location.pathname }} />;

  return (
    <div className="admin">
      <div className="admin-bar">
        <nav className="admin-nav" aria-label="Admin">
          <NavLink to="/admin" end data-testid="admin-nav-dashboard">Dashboard</NavLink>
          <NavLink to="/admin/orders" data-testid="admin-nav-orders">Orders</NavLink>
          <NavLink to="/admin/stock" data-testid="admin-nav-stock">Stock</NavLink>
        </nav>
        <div className="admin-user">
          <span data-testid="admin-user">{user.displayName} ({user.role})</span>
          <button type="button" className="link-button" onClick={logout} data-testid="logout">Sign out</button>
        </div>
      </div>
      {user.role === 'Support' && (
        <p className="alert alert-info" data-testid="read-only-notice">You have read-only access.</p>
      )}
      <Outlet />
    </div>
  );
}

export function LoginPage() {
  const { user, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<unknown>(null);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [busy, setBusy] = useState(false);

  if (user) return <Navigate to="/admin" replace />;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setFieldErrors({});
    try {
      await login(username, password);
      const from = (location.state as { from?: string } | null)?.from;
      navigate(from ?? '/admin', { replace: true });
    } catch (e) {
      if (e instanceof ApiError && e.status === 400) setFieldErrors(e.fieldErrors);
      else setError(e);
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={submit} noValidate data-testid="login-form">
        <h1>Staff login</h1>
        <Field id="username" label="Username" error={fieldErrors.username}>
          <input id="username" autoComplete="username" value={username} onChange={(e) => setUsername(e.target.value)} />
        </Field>
        <Field id="password" label="Password" error={fieldErrors.password}>
          <input id="password" type="password" autoComplete="current-password" value={password}
            onChange={(e) => setPassword(e.target.value)} />
        </Field>
        <ErrorMessage error={error} testId="login-error" />
        <button type="submit" className="button button-primary button-block" disabled={busy} data-testid="login-button">
          {busy ? 'Signing in...' : 'Sign in'}
        </button>
      </form>
    </div>
  );
}
