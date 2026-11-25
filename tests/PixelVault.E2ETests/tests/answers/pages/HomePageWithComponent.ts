import { type Locator, type Page } from '@playwright/test';
import { Header } from '../components/header';

export class HomePage {
  private readonly page: Page;
  readonly header: Header;

  constructor(page: Page) {
    this.page = page;
    this.header = new Header(page);
  }

  async goto() {
    await this.page.goto('/');
  }

  async searchFor(searchTerm: string) {
    await this.page.getByLabel('Search').fill(searchTerm);
    await this.page.getByRole('button', { name: 'Go' }).click();
  }

  async openGame(title: string) {
    await this.page.getByRole('link', { name: title, exact: true }).click();
  }
}
