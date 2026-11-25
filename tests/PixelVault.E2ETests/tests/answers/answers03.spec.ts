import { test, expect } from '@playwright/test';
import { HomePage } from './pages/HomePage';
import { GameDetailPage } from './pages/GameDetailPage';
import { BasketPage } from './pages/BasketPage';

/*
 * TODO: Refactor these two tests into a single, parameterized test that takes
 * the game title, number of copies, and expected total as parameters.
 * 
 * Create the test data as an array of objects,
 * then use the test.describe() and test() methods to create a parameterized test.
 */

test.describe('Adding games to the basket', () => {
  
  const testData = [
    { title: 'Ashes of Valdren', copies: 2, expectedTotal: '$119.98' },
    { title: 'Neon Drift 2088', copies: 1, expectedTotal: '$37.49' }
  ];

  for (const { title, copies, expectedTotal } of testData) {
    test(`Adding ${copies} copies of ${title} to the basket yields a total of ${expectedTotal}`, async ({ page }) => {
      const homePage = new HomePage(page);
      const gameDetailPage = new GameDetailPage(page);
      const basketPage = new BasketPage(page);

      await homePage.goto();
      await homePage.searchFor(title);
      await homePage.openGame(title);

      const numberOfCopies = copies;
      await gameDetailPage.addToBasket(numberOfCopies);
      await expect(gameDetailPage.basketCount).toHaveText(numberOfCopies.toString());

      await gameDetailPage.goToBasket();

      await expect(basketPage.lineTitle).toHaveText(title);
      await expect(basketPage.lineQuantity).toHaveValue(numberOfCopies.toString());
      await expect(basketPage.lineTotal).toHaveText(expectedTotal);
      await expect(basketPage.total).toHaveText(expectedTotal);
    });
  }
});
