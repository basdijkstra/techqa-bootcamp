import { test, expect } from '@playwright/test';

/*
 * TODO: write a test that searches for a game and checks the search results:
 *
 * 1. Go to the home page.
 * 2. Type 'Valdren' in the search field. Find the field by its label ('Search').
 * 3. Click the 'Go' button. Find the button by its role and name.
 * 4. Check that the result count (data-testid 'result-count') shows '1 game'.
 * 5. Check that there is exactly one game card (data-testid 'game-card').
 * 6. Check that the title of the game is 'Ashes of Valdren'. Find the locator yourself.
 */
test('Searching for Valdren finds Ashes of Valdren', async ({ page }) => {

});
