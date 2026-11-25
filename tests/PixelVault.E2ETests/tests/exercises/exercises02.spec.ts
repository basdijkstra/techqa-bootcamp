import { test, expect } from '@playwright/test';
import { HomePage } from './pages/HomePage';

test('Searching for Valdren and adding 2 copies to the basket', async ({ page }) => {

  const homePage = new HomePage(page);

  /*
   * TODO: Replace the five statements below with the equivalent code using the HomePage class.
   * This class has been provided for you in the pages directory,
   * and all of its methods have been implemented for you already.
   */
  await page.goto('/');

  await expect(page.getByTestId('basket-count')).toHaveText('0');
  await page.getByLabel('Search').fill('Valdren');
  await page.getByRole('button', { name: 'Go' }).click();

  await page.getByRole('link', { name: 'Ashes of Valdren', exact: true }).click();

  /*
   * TODO: Replace the four statements below with the equivalent code using the GameDetailPage class.
   * This class has been provided for you in the pages directory.
   * You will need to implement one method in the GameDetailPage class yourself to complete this task.
   */
  await page.getByLabel('Quantity').selectOption('2');
  await page.getByRole('button', { name: 'Add to basket' }).click();
  await expect(page.getByTestId('basket-count')).toHaveText('2');

  await page.getByRole('link', { name: 'View basket' }).click();

  /*
   * TODO: Replace the five statements below with the equivalent code using the BasketPage class.
   * You will need to implement the BasketPage class yourself to complete this task.
   * Since we're only reading values from the page, you will only need to implement
   * a constructor and the five properties below for this class, no methods are required.
   */
  await expect(page.getByTestId('basket-line')).toHaveCount(1);
  await expect(page.getByTestId('line-title')).toHaveText('Ashes of Valdren');
  await expect(page.getByTestId('line-quantity')).toHaveValue('2');
  await expect(page.getByTestId('line-total')).toHaveText('$119.98');
  await expect(page.getByTestId('basket-total')).toHaveText('$119.98');
});
