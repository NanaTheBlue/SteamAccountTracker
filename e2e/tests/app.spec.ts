import { test, expect } from '@playwright/test';

test('has title', async ({ page }) => {
  await page.goto('/');

  // Expect a title "to contain" a substring.
  await expect(page).toHaveTitle(/CheaterWatch/);
});

test('can navigate to login page', async ({ page }) => {
  await page.goto('/');

  // Click the Login link in the navbar
  await page.getByRole('navigation').getByRole('link', { name: 'Log in' }).click();

  // Expects the URL to contain login
  await expect(page).toHaveURL(/.*login/);

  // Expect a login form header
  await expect(page.getByRole('heading', { level: 2 })).toContainText(/sign in/i);
});
