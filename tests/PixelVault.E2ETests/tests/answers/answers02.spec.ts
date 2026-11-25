import { test, expect } from '@playwright/test';
import { HomePage } from './pages/HomePage';
import { GameDetailPage } from './pages/GameDetailPage';
import { BasketPage } from './pages/BasketPage';

test('Searching for Valdren and adding 2 copies to the basket', async ({ page }) => {

  const homePage = new HomePage(page);
  const gameDetailPage = new GameDetailPage(page);
  const basketPage = new BasketPage(page);

  /*
   * TODO: Replace the five statements below with the equivalent code using the HomePage class.
   * This class has been provided for you in the pages directory,
   * and all of its methods have been implemented for you already.
   */
  await homePage.goto();
  await expect(homePage.basketCount).toHaveText('0');

  await homePage.searchFor('Valdren');
  await homePage.openGame('Ashes of Valdren');

  /*
   * TODO: Replace the four statements below with the equivalent code using the GameDetailPage class.
   * This class has been provided for you in the pages directory.
   * You will need to implement one method in the GameDetailPage class yourself to complete this task.
   */
  await gameDetailPage.addToBasket(2);
  await expect(gameDetailPage.basketCount).toHaveText('2');

  await gameDetailPage.goToBasket();

  /*
   * TODO: Replace the five statements below with the equivalent code using the BasketPage class.
   * You will need to implement the BasketPage class yourself to complete this task.
   * Since we're only reading values from the page, you will only need to implement
   * a constructor and the five properties below for this class, no methods are required.
   */
  await expect(basketPage.lines).toHaveCount(1);
  await expect(basketPage.lineTitle).toHaveText('Ashes of Valdren');
  await expect(basketPage.lineQuantity).toHaveValue('2');
  await expect(basketPage.lineTotal).toHaveText('$119.98');
  await expect(basketPage.total).toHaveText('$119.98');
});
