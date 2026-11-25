import { type Locator, type Page } from '@playwright/test';

export class BasketPage {
  readonly lines: Locator;
  readonly lineTitle: Locator;
  readonly lineQuantity: Locator;
  readonly lineTotal: Locator;
  readonly total: Locator;

  constructor(page: Page) {
    this.lines = page.getByTestId('basket-line');
    this.lineTitle = page.getByTestId('line-title');
    this.lineQuantity = page.getByTestId('line-quantity');
    this.lineTotal = page.getByTestId('line-total');
    this.total = page.getByTestId('basket-total');
  }
}
