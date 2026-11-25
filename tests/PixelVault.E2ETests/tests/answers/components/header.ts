import { Locator, Page } from "@playwright/test";

export class Header {

    private readonly page: Page;
    readonly basketCount: Locator;

    constructor(page: Page) {
        this.page = page;
        this.basketCount = page.getByTestId('basket-count');
    }
}