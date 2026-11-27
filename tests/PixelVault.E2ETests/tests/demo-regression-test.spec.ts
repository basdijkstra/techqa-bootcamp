import { test, expect, type Locator, type Page } from '@playwright/test';

test.use({ launchOptions: { slowMo: 500 } });

test('Paying with a declined card shows an error and does not place the order', async ({ page }) => {

  const catalogPage = new CatalogPage(page);
  const gameDetailsPage = new GameDetailsPage(page);
  const basketPage = new BasketOverviewPage(page);
  const shippingPage = new ShippingDetailsPage(page);
  const paymentPage = new PaymentPage(page);

  // Find a single game and open it.
  await catalogPage.open();
  await catalogPage.searchFor('Tidebound');
  await expect(catalogPage.results()).toHaveCount(1);
  await catalogPage.openGame('Tidebound');

  // Put one copy in the basket and check the basket contents.
  await gameDetailsPage.addToBasket();
  await gameDetailsPage.goToBasket();
  await expect(basketPage.lines).toHaveCount(1);
  await expect(basketPage.lines.getByTestId('line-title')).toHaveText('Tidebound');
  await expect(basketPage.lines.getByTestId('line-quantity')).toHaveValue('1');

  // Checkout, step 1: shipping details.
  await basketPage.proceedToCheckout();
  await shippingPage.enterDetails({
    fullName: 'Demo Customer',
    email: 'demo.customer@example.com',
    phone: '(212) 555-0123',
    line1: '350 5th Ave',
    city: 'New York',
    state: 'NY',
    zipCode: '10118',
  });
  await shippingPage.continueWithDefaultShipping();

  // Checkout, step 2: pay with 4000 0000 0000 0002, a test card that the payment provider always declines.
  // The expiry date must be in the future, so we use next year.
  await paymentPage.payWith({
    number: '4000000000000002',
    expiryMonth: '12',
    expiryYear: String(new Date().getFullYear() + 1),
    cvv: '123',
  });

  // The payment is declined: the customer sees an error and stays on the payment page.
  await expect(paymentPage.paymentError).toBeVisible();
  await expect(paymentPage.paymentError).toHaveAttribute('data-error-code', 'PAYMENT_DECLINED');
  await expect(paymentPage.paymentError).toContainText('declined');
  await expect(page).toHaveURL(/\/checkout\/payment$/);
});


class CatalogPage {
  private readonly searchInput: Locator;
  private readonly searchButton: Locator;
  private readonly gameCards: Locator;

  constructor(private readonly page: Page) {
    this.searchInput = page.getByTestId('search-input');
    this.searchButton = page.getByTestId('search-button');
    this.gameCards = page.getByTestId('game-card');
  }

  async open() {
    // The baseURL (http://localhost:5173) is set in playwright.config.ts, so '/' is the home page.
    await this.page.goto('/');
  }

  async searchFor(text: string) {
    await this.searchInput.fill(text);
    await this.searchButton.click();
  }

  /** The game cards currently shown, so the test can check the search result. */
  results(): Locator {
    return this.gameCards;
  }

  async openGame(title: string) {
    await this.gameCards.getByTestId('game-title').filter({ hasText: title }).click();
  }
}

/** The page for a single game, where it can be added to the basket. */
class GameDetailsPage {
  private readonly addToBasketButton: Locator;
  private readonly viewBasketLink: Locator;

  constructor(private readonly page: Page) {
    this.addToBasketButton = page.getByTestId('add-to-basket');
    this.viewBasketLink = page.getByTestId('go-to-basket');
  }

  /** Adds the game to the basket. The quantity selector defaults to 1, so this adds a single copy. */
  async addToBasket() {
    await this.addToBasketButton.click();
  }

  async goToBasket() {
    // This link only appears once the game was added, so clicking it also waits for that.
    await this.viewBasketLink.click();
  }
}

/** The basket, with its lines and the button to start checking out. */
class BasketOverviewPage {
  readonly lines: Locator;
  readonly total: Locator;
  private readonly checkoutButton: Locator;

  constructor(page: Page) {
    this.lines = page.getByTestId('basket-line');
    this.total = page.getByTestId('basket-total');
    this.checkoutButton = page.getByTestId('checkout-button');
  }

  async proceedToCheckout() {
    await this.checkoutButton.click();
  }
}

/** Contact details and address used in the shipping step. */
type Customer = {
  fullName: string;
  email: string;
  phone: string;
  line1: string;
  city: string;
  state: string;
  zipCode: string;
};

/** Checkout step 1: contact details, shipping address and shipping method. */
class ShippingDetailsPage {
  private readonly calculateShippingButton: Locator;
  private readonly continueToPaymentButton: Locator;

  constructor(private readonly page: Page) {
    this.calculateShippingButton = page.getByTestId('calculate-shipping');
    this.continueToPaymentButton = page.getByTestId('continue-to-payment');
  }

  async enterDetails(customer: Customer) {
    // The form fields have proper labels, so we can find them the way a user would: by their label.
    await this.page.getByLabel('Full name').fill(customer.fullName);
    await this.page.getByLabel('Email address').fill(customer.email);
    await this.page.getByLabel('Phone number').fill(customer.phone);
    await this.page.getByLabel('Address line 1').fill(customer.line1);
    await this.page.getByLabel('City').fill(customer.city);
    await this.page.getByLabel('State').selectOption(customer.state);
    await this.page.getByLabel('ZIP code').fill(customer.zipCode);
  }

  /** Gets a shipping quote and continues with the shipping method that is selected first (Standard). */
  async continueWithDefaultShipping() {
    await this.calculateShippingButton.click();
    await this.continueToPaymentButton.click();
  }
}

/** Card details used in the payment step. */
type Card = {
  number: string;
  expiryMonth: string;
  expiryYear: string;
  cvv: string;
};

/** Checkout step 2: card details and placing the order. */
class PaymentPage {
  readonly paymentError: Locator;
  private readonly placeOrderButton: Locator;

  constructor(private readonly page: Page) {
    this.paymentError = page.getByTestId('payment-error');
    this.placeOrderButton = page.getByTestId('place-order');
  }

  async payWith(card: Card) {
    // The name on the card is already filled in with the customer's full name from the shipping step.
    await this.page.getByLabel('Card number').fill(card.number);
    await this.page.getByLabel('Expiry month').selectOption(card.expiryMonth);
    await this.page.getByLabel('Expiry year').selectOption(card.expiryYear);
    await this.page.getByLabel('Security code').fill(card.cvv);

    // The button is disabled until the order totals are calculated. Playwright waits for it to be enabled.
    await this.placeOrderButton.click();
  }
}
