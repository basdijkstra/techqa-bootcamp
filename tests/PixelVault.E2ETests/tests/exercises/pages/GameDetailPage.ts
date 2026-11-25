import { type Locator, type Page } from '@playwright/test';

export class GameDetailPage {
  private readonly page: Page;
  readonly basketCount: Locator;

  constructor(page: Page) {
    this.page = page;
    this.basketCount = page.getByTestId('basket-count');
  }

  async addToBasket(quantity: number) {
    await this.page.getByLabel('Quantity').selectOption(String(quantity));
    await this.page.getByRole('button', { name: 'Add to basket' }).click();
  }

  /*
   * TODO: Add a 'goToBasket' method to click the "View basket" link on the page.
   */

}
