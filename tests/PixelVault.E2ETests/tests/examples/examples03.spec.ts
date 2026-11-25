import { test, expect } from '@playwright/test';
import {LoginPage} from "./pages/loginPage";
import { ProductsOverviewPage } from './pages/productsOverviewPage';
import { ProductDetailPage } from './pages/productDetailPage';

test('Add a Sauce Labs Backpack to the basket', async ({ page }) => {

  const loginPage = new LoginPage(page);
  await loginPage.open();
  await loginPage.loginAs('standard_user', 'secret_sauce');

  const productsOverviewPage = new ProductsOverviewPage(page);
  await productsOverviewPage.selectProduct('Sauce Labs Backpack');

  await expect(new ProductDetailPage(page).buttonAddToCart).toBeVisible();
});

test('Add a Sauce Labs Bike Light to the basket', async ({ page }) => {

  const loginPage = new LoginPage(page);
  await loginPage.open();
  await loginPage.loginAs('standard_user', 'secret_sauce');

  const productsOverviewPage = new ProductsOverviewPage(page);
  await productsOverviewPage.selectProduct('Sauce Labs Bike Light');

  await expect(new ProductDetailPage(page).buttonAddToCart).toBeVisible();
});

const products = [
  { name: 'Sauce Labs Backpack' },
  { name: 'Sauce Labs Bike Light' }
];

test.describe('Add products to the basket', () => {
  for (const product of products) {
    test(`Add ${product.name} to the basket`, async ({ page }) => {
      const loginPage = new LoginPage(page);
      await loginPage.open();
      await loginPage.loginAs('standard_user', 'secret_sauce');

      const productsOverviewPage = new ProductsOverviewPage(page);
      await productsOverviewPage.selectProduct('Sauce Labs Bike Light');

      await expect(new ProductDetailPage(page).buttonAddToCart).toBeVisible();
    });
  }
});
