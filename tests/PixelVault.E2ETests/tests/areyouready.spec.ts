import { test, expect } from '@playwright/test';

test('PixelVault is ready', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('logo')).toBeVisible();

  await page.getByRole('link', { name: 'Ashes of Valdren', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Add to basket' })).toBeVisible();
});
