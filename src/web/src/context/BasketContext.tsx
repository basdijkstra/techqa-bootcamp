import { createContext, useCallback, useContext, type ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { basketApi } from '../api/api';
import { ApiError } from '../api/http';
import type { Basket } from '../api/types';

const BASKET_KEY = 'pv.basketId';

interface BasketContextValue {
  basket: Basket | undefined;
  isLoading: boolean;
  addItem: (gameId: number, quantity: number) => Promise<Basket>;
  updateItem: (gameId: number, quantity: number) => Promise<Basket>;
  removeItem: (gameId: number) => Promise<Basket>;
  applyPromoCode: (code: string) => Promise<Basket>;
  removePromoCode: () => Promise<Basket>;
  /** Forgets the current basket, e.g. after an order was placed. */
  startNewBasket: () => void;
}

const BasketContext = createContext<BasketContextValue | null>(null);

/** Loads the basket from local storage, or creates a new one when it is missing, expired or checked out. */
async function loadOrCreateBasket(): Promise<Basket> {
  const id = localStorage.getItem(BASKET_KEY);
  if (id) {
    try {
      const basket = await basketApi.get(id);
      if (basket.status === 'Active') return basket;
    } catch (error) {
      if (!(error instanceof ApiError) || ![400, 404, 410].includes(error.status)) throw error;
    }
  }

  const basket = await basketApi.create();
  localStorage.setItem(BASKET_KEY, basket.id);
  return basket;
}

export function BasketProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient();
  const { data: basket, isLoading } = useQuery({ queryKey: ['basket'], queryFn: loadOrCreateBasket });

  const update = useCallback(
    async (action: (basketId: string) => Promise<Basket>) => {
      const current: Basket = basket ?? (await queryClient.fetchQuery({ queryKey: ['basket'], queryFn: loadOrCreateBasket }));
      try {
        const updated = await action(current.id);
        queryClient.setQueryData(['basket'], updated);
        return updated;
      } catch (error) {
        if (error instanceof ApiError && error.status === 410) {
          localStorage.removeItem(BASKET_KEY);
          await queryClient.invalidateQueries({ queryKey: ['basket'] });
        }
        throw error;
      }
    },
    [basket, queryClient],
  );

  const value: BasketContextValue = {
    basket,
    isLoading,
    addItem: (gameId, quantity) => update((id) => basketApi.addItem(id, gameId, quantity)),
    updateItem: (gameId, quantity) => update((id) => basketApi.updateItem(id, gameId, quantity)),
    removeItem: (gameId) => update((id) => basketApi.removeItem(id, gameId)),
    applyPromoCode: (code) => update((id) => basketApi.applyPromoCode(id, code)),
    removePromoCode: () => update((id) => basketApi.removePromoCode(id)),
    startNewBasket: () => {
      localStorage.removeItem(BASKET_KEY);
      void queryClient.invalidateQueries({ queryKey: ['basket'] });
    },
  };

  return <BasketContext.Provider value={value}>{children}</BasketContext.Provider>;
}

export function useBasket() {
  const context = useContext(BasketContext);
  if (!context) throw new Error('useBasket must be used inside BasketProvider');
  return context;
}
