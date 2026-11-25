import { test, expect } from '@playwright/test';

test('Example of API mocking in Playwright', async ({ page }) => {

  await page.route('*/**/api/v1/fruits', async route => {
    const responseBody = [{name: 'Jackfruit', id: 21}];
    await route.fulfill({status: 200, json: responseBody})
  });

  await page.goto('https://demo.playwright.dev/api-mocking');

  // Assert that the Jackfruit fruit is visible
  await expect(page.getByText('Jackfruit')).toBeVisible();
});
