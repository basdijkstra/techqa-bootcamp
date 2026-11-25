import { type Page } from '@playwright/test';
import { Header } from '../components/header';

export class GameDetailPage {
  private readonly page: Page;
  readonly header: Header;

  constructor(page: Page) {
    this.page = page;
    this.header = new Header(page);
  }

  async addToBasket(quantity: number) {
    await this.page.getByLabel('Quantity').selectOption(String(quantity));
    await this.page.getByRole('button', { name: 'Add to basket' }).click();
  }

  /*
   * TODO: Add a 'goToBasket' method to click the "View basket" link on the page.
   */
  async goToBasket() {
    await this.page.getByRole('link', { name: 'View basket' }).click();
  }
}
