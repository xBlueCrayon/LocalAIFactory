import { test, expect } from '@playwright/test';

// Proves the REAL (PBKDF2) login flow: bad password rejected, good password signs in and reaches the dashboard.
test('real login rejects a wrong password', async ({ page }) => {
  await page.goto('/Account/Login');
  await expect(page.getByTestId('login-form')).toBeVisible();
  await page.getByTestId('username').fill('admin');
  await page.getByTestId('password').fill('wrong-password');
  await page.getByTestId('login-submit').click();
  await expect(page.getByTestId('login-error')).toBeVisible();
});

test('real login with the seeded admin reaches the dashboard', async ({ page }) => {
  await page.goto('/Account/Login');
  await page.getByTestId('username').fill('admin');
  await page.getByTestId('password').fill('Admin#12345');
  await page.getByTestId('login-submit').click();
  await expect(page).toHaveURL(/\/$|\/Home/);
  await expect(page.getByTestId('dashboard')).toBeVisible();
});
