import { test, expect } from '@playwright/test';
import { BasketPage } from '../answers/pages/BasketPage';
import { MockResponses } from '../answers/responses/mockResponses';

const basketId = '11111111-2222-3333-4444-555555555555';

test('A price change since adding a game to the basket is shown to the customer', async ({ page }) => {

  const basketPage = new BasketPage(page);

  /*
   * TODO: Use the Playwright API client to mock the response from the /baskets endpoint of the API.
   * The response should be sent when the browser makes a GET request to
   * 'http://localhost:5102/baskets/{basketId}'. 
   * You can use a wildcard in the URL to match any basket ID.
   * 
   * The response should be a JSON object that is returned by the
   * MockResponses.getBasketResponseWithPriceChangedForItem() method.
   * Pass in the basketId variable to that method to get the correct response.
   * The response should have a status code of 200 OK.
   */


  /*
   * After the API calls have been made as described above,
   * the basket page should show the correct message, line price change, and total.
   * The checkout button should be enabled.
   */
  await page.addInitScript(id => localStorage.setItem('pv.basketId', id), basketId);
  await page.goto('/basket');

  await expect(page.getByTestId('basket-message')).toHaveText('The price of one or more items has changed since you added them.');
  await expect(page.getByTestId('line-unit-price')).toContainText('$54.99 each');
  await expect(page.getByTestId('line-price-changed')).toHaveText('(was $59.99)');
  await expect(basketPage.lineTotal).toHaveText('$109.98');
  await expect(basketPage.total).toHaveText('$109.98');

  await expect(page.getByTestId('checkout-button')).toBeEnabled();
});

test('A basket with more copies of a game than are in stock cannot be checked out', async ({ page }) => {

  const basketPage = new BasketPage(page);

  /*
   * TODO: Use the Playwright API client to mock the response from the /baskets endpoint of the API.
   * The response should be sent when the browser makes a GET request to
   * 'http://localhost:5102/baskets/{basketId}'. 
   * You can use a wildcard in the URL to match any basket ID.
   * 
   * The response should be a JSON object that is returned by the
   * MockResponses.getBasketResponseWithStockIssue() method.
   * Pass in the basketId variable to that method to get the correct response.
   * The response should have a status code of 200 OK.
   */


  /*
   * After the API calls have been made as described above,
   * the basket page should show the correct message, line issue, and total.
   * The checkout button should be disabled.
   */
  await page.addInitScript(id => localStorage.setItem('pv.basketId', id), basketId);
  await page.goto('/basket');

  await expect(page.getByTestId('basket-message')).toHaveText('Some items in your basket need attention before you can check out.');
  await expect(page.getByTestId('line-issue')).toHaveText('Only 1 left in stock. Please lower the quantity.');
  await expect(basketPage.lineTotal).toHaveText('$179.97');
  await expect(basketPage.total).toHaveText('$179.97');

  await expect(page.getByTestId('checkout-button')).toBeDisabled();
});

test('An error message is shown when the basket cannot be loaded', async ({ page }) => {

  /*
   * TODO: Use the Playwright API client to mock the response from the /baskets endpoint of the API.
   * The response should be sent when the browser makes a GET request to
   * 'http://localhost:5102/baskets/{basketId}'.
   * You can use a wildcard in the URL to match any basket ID.
   *
   * The response should be a JSON object that is returned by the
   * MockResponses.getInternalServerErrorResponse() method.
   * The response should have a status code of 500 Internal Server Error.
   */


  /*
   * After the API calls have been made as described above,
   * the basket page should show an error message instead of the basket contents.
   * The checkout button should not be shown at all.
   */
  await page.addInitScript(id => localStorage.setItem('pv.basketId', id), basketId);
  await page.goto('/basket');

  await expect(page.getByTestId('basket-error')).toHaveText('An error occurred while processing your request.');
  await expect(page.getByTestId('checkout-button')).toBeHidden();
});
