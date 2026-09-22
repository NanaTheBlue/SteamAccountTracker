import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test('can register, logout, login, and delete account', async ({ page }) => {
    const userEmail = `playwright-${Date.now()}@example.com`;
    const userPassword = 'TestPassword123!';

    // 1. Register (which auto-logs us in)
    await page.goto('/register');
    await page.fill('input[type="text"]', 'PlaywrightUser');
    await page.fill('input[type="email"]', userEmail);
    await page.fill('input[type="password"]', userPassword);
    await page.click('button[type="submit"]');

    // Registration should auto-login and redirect to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('text=Hello, PlaywrightUser')).toBeVisible();

    // 2. Logout to test the login screen
    await page.click('text=Logout');
    await expect(page).toHaveURL(/.*login/);
    await expect(page.locator('text=Hello, PlaywrightUser')).not.toBeVisible();

    // 3. Login with the newly created credentials
    await page.fill('input[type="email"]', userEmail);
    await page.fill('input[type="password"]', userPassword);
    await page.click('button[type="submit"]');

    // Wait for login and redirect back to dashboard
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('text=Hello, PlaywrightUser')).toBeVisible();

    // 4. Navigate to settings and delete the account
    await page.goto('/settings');
    
    // Automatically accept the browser confirmation dialog
    page.on('dialog', dialog => dialog.accept());
    await page.click('text=Delete Account');

    // Wait for redirect to home page
    await expect(page).toHaveURL(/.*$/);
    
    // Ensure we are logged out by checking navbar
    await expect(page.locator('text=Login')).toBeVisible();
  });
});

