import { test, expect } from '@playwright/test';
import { HomePage } from './pages/HomePageWithComponent';
import { GameDetailPage } from './pages/GameDetailPageWithComponent';
import { BasketPage } from './pages/BasketPage';

test('Searching for Valdren and adding 2 copies to the basket', async ({ page }) => {

  const homePage = new HomePage(page);
  const gameDetailPage = new GameDetailPage(page);
  const basketPage = new BasketPage(page);

  await homePage.goto();
  await expect(homePage.header.basketCount).toHaveText('0');

  await homePage.searchFor('Valdren');
  await homePage.openGame('Ashes of Valdren');

  await gameDetailPage.addToBasket(2);
  await expect(gameDetailPage.header.basketCount).toHaveText('2');

  await gameDetailPage.goToBasket();

  await expect(basketPage.lines).toHaveCount(1);
  await expect(basketPage.lineTitle).toHaveText('Ashes of Valdren');
  await expect(basketPage.lineQuantity).toHaveValue('2');
  await expect(basketPage.lineTotal).toHaveText('$119.98');
  await expect(basketPage.total).toHaveText('$119.98');
});
