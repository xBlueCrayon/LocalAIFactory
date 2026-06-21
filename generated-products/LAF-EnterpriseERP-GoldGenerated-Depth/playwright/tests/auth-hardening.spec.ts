import { test, expect } from '@playwright/test';

// Real-auth hardening proven through the browser + HTTP: lockout messaging and anti-forgery enforcement.

test('repeated wrong passwords lock the account', async ({ page }) => {
  for (let i = 0; i < 6; i++) {
    await page.goto('/Account/Login');
    await page.getByTestId('username').fill('alice');
    await page.getByTestId('password').fill(`wrong-${i}`);
    await page.getByTestId('login-submit').click();
  }
  await expect(page.getByTestId('login-error')).toContainText(/lock/i);
});

test('login POST without an anti-forgery token is rejected', async ({ request }) => {
  // Direct POST bypassing the rendered token must fail anti-forgery validation (400).
  const resp = await request.post('/Account/Login', {
    form: { username: 'admin', password: 'Admin#12345' },
    maxRedirects: 0,
  });
  expect(resp.status()).toBe(400);
});

test('login page renders an anti-forgery token field', async ({ page }) => {
  await page.goto('/Account/Login');
  await expect(page.locator('input[name="__RequestVerificationToken"]')).toHaveCount(1);
});
