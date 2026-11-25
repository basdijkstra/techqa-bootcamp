import { Locator, Page } from '@playwright/test';

export class ProductsOverviewPage {

    private readonly page: Page;
    readonly title: Locator;

    constructor(page: Page) {
        this.page = page;
        this.title = page.locator('[data-test=title]');
    }

    async open() {
        await this.page.goto('https://www.saucedemo.com/inventory.html');
    }
    
    async selectProduct(product: string) {
        await this.page.getByText(product).click();
    }
}