// Small fetch wrapper shared by all API modules. Errors from the services are RFC 7807 problem details
// with an extra "code" (e.g. OUT_OF_STOCK) and, for validation errors, an "errors" object per field.

declare const __CATALOG_URL__: string;
declare const __BASKET_URL__: string;
declare const __ORDERS_URL__: string;
declare const __PAYMENT_URL__: string;
declare const __SHIPPING_URL__: string;
declare const __IDENTITY_URL__: string;

export const serviceUrls = {
  catalog: __CATALOG_URL__,
  basket: __BASKET_URL__,
  orders: __ORDERS_URL__,
  payment: __PAYMENT_URL__,
  shipping: __SHIPPING_URL__,
  identity: __IDENTITY_URL__,
};

export type Service = keyof typeof serviceUrls;

export interface ProblemDetails {
  status: number;
  title?: string;
  detail?: string;
  code?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly problem: ProblemDetails;

  constructor(problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${problem.status}`);
    this.status = problem.status;
    this.code = problem.code ?? 'ERROR';
    this.problem = problem;
  }

  /** Field errors from a 400 response, e.g. { "customer.email": ["Email address is not valid."] }. */
  get fieldErrors(): Record<string, string[]> {
    return this.problem.errors ?? {};
  }
}

const TOKEN_KEY = 'pv.adminToken';

export function getToken(): string | null {
  return sessionStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null) {
  if (token) {
    sessionStorage.setItem(TOKEN_KEY, token);
  } else {
    sessionStorage.removeItem(TOKEN_KEY);
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  auth?: boolean;
  query?: Record<string, string | number | boolean | string[] | undefined | null>;
}

export async function request<T>(service: Service, path: string, options: RequestOptions = {}): Promise<T> {
  const url = new URL(path, serviceUrls[service]);
  for (const [key, value] of Object.entries(options.query ?? {})) {
    if (value === undefined || value === null || value === '' || value === false) continue;
    if (Array.isArray(value)) {
      value.forEach((v) => url.searchParams.append(key, v));
    } else {
      url.searchParams.set(key, String(value));
    }
  }

  const headers: Record<string, string> = { Accept: 'application/json' };
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';
  const token = getToken();
  if (options.auth && token) headers.Authorization = `Bearer ${token}`;

  let response: Response;
  try {
    response = await fetch(url, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    });
  } catch {
    throw new ApiError({
      status: 0,
      code: 'NETWORK_ERROR',
      detail: `Could not reach the ${service} service. Is it running?`,
    });
  }

  if (response.status === 204) return undefined as T;

  const text = await response.text();
  const data = text ? JSON.parse(text) : undefined;

  if (!response.ok) {
    if (response.status === 401 && options.auth) {
      setToken(null);
      window.dispatchEvent(new Event('pv:unauthorized'));
    }
    throw new ApiError({ status: response.status, ...(data ?? {}) });
  }

  return data as T;
}
