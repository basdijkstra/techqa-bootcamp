import { createContext, useContext, useEffect, useState, type ReactNode } from 'react';
import { identityApi } from '../api/api';
import { setToken } from '../api/http';
import type { StaffUser } from '../api/types';

const USER_KEY = 'pv.adminUser';

interface AuthContextValue {
  user: StaffUser | null;
  isAdmin: boolean;
  login: (username: string, password: string) => Promise<StaffUser>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function readStoredUser(): StaffUser | null {
  const stored = sessionStorage.getItem(USER_KEY);
  return stored ? (JSON.parse(stored) as StaffUser) : null;
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<StaffUser | null>(readStoredUser);

  const logout = () => {
    setToken(null);
    sessionStorage.removeItem(USER_KEY);
    setUser(null);
  };

  useEffect(() => {
    // Fired by the HTTP client when a token is rejected (for example because it expired).
    const onUnauthorized = () => logout();
    window.addEventListener('pv:unauthorized', onUnauthorized);
    return () => window.removeEventListener('pv:unauthorized', onUnauthorized);
  }, []);

  const login = async (username: string, password: string) => {
    const response = await identityApi.login(username, password);
    setToken(response.accessToken);
    sessionStorage.setItem(USER_KEY, JSON.stringify(response.user));
    setUser(response.user);
    return response.user;
  };

  return (
    <AuthContext.Provider value={{ user, isAdmin: user?.role === 'Admin', login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider');
  return context;
}
