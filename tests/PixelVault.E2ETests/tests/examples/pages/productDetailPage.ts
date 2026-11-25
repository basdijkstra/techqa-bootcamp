import { Locator, Page } from "@playwright/test";

export class ProductDetailPage {

    private readonly page: Page;
    readonly buttonAddToCart: Locator;

    constructor(page: Page) {
        this.page = page;
        this.buttonAddToCart = page.locator('[data-test=add-to-cart]');
    }
}