import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test('can register, logout, login, and delete account', async ({ page }) => {
    const userEmail = `playwright-${Date.now()}@example.com`;
    const userPassword = 'TestPassword123!';
    const operator = page.getByTestId('nav-operator');

    // 1. Register (which auto-logs us in)
    await page.goto('/register');
    await page.fill('input[type="text"]', 'PlaywrightUser');
    await page.fill('input[type="email"]', userEmail);
    await page.fill('input[type="password"]', userPassword);
    await page.click('button[type="submit"]');

    // Registration should auto-login and redirect to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(operator).toContainText('PlaywrightUser');

    // 2. Logout to test the login screen
    await page.getByRole('button', { name: 'Log out' }).click();
    await expect(page).toHaveURL(/.*login/);
    await expect(operator).not.toBeVisible();

    // 3. Login with the newly created credentials
    await page.fill('input[type="email"]', userEmail);
    await page.fill('input[type="password"]', userPassword);
    await page.click('button[type="submit"]');

    // Wait for login and redirect back to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(operator).toContainText('PlaywrightUser');

    // 4. Navigate to settings and delete the account (two-step confirm button)
    await page.goto('/settings');
    await page.getByRole('button', { name: 'Delete account' }).click();
    await page.getByRole('button', { name: 'Yes, delete it' }).click();

    // Wait for redirect to home page
    await expect(page).toHaveURL(/\/$/);

    // Ensure we are logged out by checking navbar
    await expect(page.getByRole('navigation').getByRole('link', { name: 'Log in' })).toBeVisible();
  });
});
