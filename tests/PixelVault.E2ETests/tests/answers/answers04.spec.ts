import { test, expect } from '@playwright/test';
import { BasketPage } from './pages/BasketPage';

const basketApiUrl = 'http://localhost:5102';

test('A basket with 3 games in the same category gets the bundle discount', async ({ page, request }) => {

  /*
   * TODO: Use the Playwright API client to create a new basket.
   * Perform an HTTP POST request to the /baskets endpoint of the API.
   * The response will contain the basket ID in the 'id' field of the response body,
   * which you need to store in a variable so you can use that next to add items to the basket.
   * Replace const basketId = 'REPLACE_ME' with the actual basket ID from the response.
   */
  const createBasketResponse = await request.post(`${basketApiUrl}/baskets`);
  expect(createBasketResponse.status()).toBe(201);
  const basketId = (await createBasketResponse.json()).id;

  /*
   * TODO: Use the Playwright API client to add 2 copies of Tidebound (gameId: 10)
   * to the basket. Perform an HTTP POST request to the /baskets/{basketId}/items endpoint of the API.
   * The request body should look like this: { "gameId": 10, "quantity": 2 }.
   * Check that the response status is 200 OK.
   */
  const addTideboundResponse = await request.post(`${basketApiUrl}/baskets/${basketId}/items`, {
    data: { gameId: 10, quantity: 2 },
  });
  expect(addTideboundResponse.status()).toBe(200);

  /*
   * TODO: Repeat the previous step to add 1 copy of Whispering Pines (gameId: 15) to the basket.
   */
  const addWhisperingPinesResponse = await request.post(`${basketApiUrl}/baskets/${basketId}/items`, {
    data: { gameId: 15, quantity: 1 },
  });
  expect(addWhisperingPinesResponse.status()).toBe(200);

  /*
   * After the API calls have been made as described above,
   * the basket page should show the correct subtotal, discount, and total.
   */
  await page.addInitScript(id => localStorage.setItem('pv.basketId', id), basketId);
  await page.goto('/basket');

  const basketPage = new BasketPage(page);
  await expect(basketPage.lines).toHaveCount(2);
  await expect(basketPage.lineTitle).toHaveText(['Tidebound', 'Whispering Pines']);
  await expect(basketPage.lineQuantity.nth(0)).toHaveValue('2');
  await expect(basketPage.lineQuantity.nth(1)).toHaveValue('1');
  await expect(basketPage.lineTotal).toHaveText(['$59.98', '$22.99']);

  await expect(page.getByTestId('basket-subtotal')).toHaveText('$82.97');
  await expect(page.getByTestId('discount-description')).toHaveText('Bundle deal: buy 3, get the cheapest 50% off');
  await expect(page.getByTestId('basket-discount')).toHaveText('-$11.50');
  await expect(basketPage.total).toHaveText('$71.47');
});
