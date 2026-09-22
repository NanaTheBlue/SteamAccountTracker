import { test, expect } from '@playwright/test';

test('has title', async ({ page }) => {
  await page.goto('/');

  // Expect a title "to contain" a substring.
  // Assuming the landing page has "CheaterWatch" in the title or heading
  await expect(page).toHaveTitle(/CheaterWatch/);
});

test('can navigate to login page', async ({ page }) => {
  await page.goto('/');

  // Click the Login link
  await page.click('text=Login');

  // Expects the URL to contain login
  await expect(page).toHaveURL(/.*login/);
  
  // Expect a login form header
  await expect(page.locator('h1')).toContainText('Login');
});

