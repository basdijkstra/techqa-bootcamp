import { test, expect } from '@playwright/test';
import { HomePage } from './pages/HomePage';
import { GameDetailPage } from '../answers/pages/GameDetailPage';
import { BasketPage } from '../answers/pages/BasketPage';

/*
 * TODO: Refactor these two tests into a single, parameterized test that takes
 * the game title, number of copies, and expected total as parameters.
 * 
 * Create the test data as an array of objects,
 * then use the test.describe() and test() methods to create a parameterized test.
 */
test('Adding 2 copies of Ashes of Valdren to the basket yields a total of $119.98', async ({ page }) => {

  const homePage = new HomePage(page);
  const gameDetailPage = new GameDetailPage(page);
  const basketPage = new BasketPage(page);

  await homePage.goto();
  await homePage.searchFor('Ashes of Valdren');
  await homePage.openGame('Ashes of Valdren');

  const numberOfCopies = 2;
  await gameDetailPage.addToBasket(numberOfCopies);
  await expect(gameDetailPage.basketCount).toHaveText(numberOfCopies.toString());

  await gameDetailPage.goToBasket();

  await expect(basketPage.lineTitle).toHaveText('Ashes of Valdren');
  await expect(basketPage.lineQuantity).toHaveValue(numberOfCopies.toString());
  await expect(basketPage.lineTotal).toHaveText('$119.98');
  await expect(basketPage.total).toHaveText('$119.98');
});

test('Adding 1 copies of Neon Drift 2088 to the basket yields a total of $37.49', async ({ page }) => {

  const homePage = new HomePage(page);
  const gameDetailPage = new GameDetailPage(page);
  const basketPage = new BasketPage(page);

  await homePage.goto();
  await homePage.searchFor('Neon Drift 2088');
  await homePage.openGame('Neon Drift 2088');

  const numberOfCopies = 1;
  await gameDetailPage.addToBasket(numberOfCopies);
  await expect(gameDetailPage.basketCount).toHaveText(numberOfCopies.toString());

  await gameDetailPage.goToBasket();

  await expect(basketPage.lineTitle).toHaveText('Neon Drift 2088');
  await expect(basketPage.lineQuantity).toHaveValue(numberOfCopies.toString());
  await expect(basketPage.lineTotal).toHaveText('$37.49');
  await expect(basketPage.total).toHaveText('$37.49');
});
