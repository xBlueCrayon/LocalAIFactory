import { test, expect } from '@playwright/test';

const pages: [string, string][] = [
  ['/', 'dashboard'],
  ['/Home/Customers', 'customers-table'],
  ['/Home/Items', 'items-table'],
  ['/Home/SalesInvoices', 'sales-invoices-table'],
  ['/Home/GeneralLedger', 'gl-table'],
  ['/Home/StockBalance', 'stock-balance-table'],
  ['/Home/WorkflowInbox', 'workflow-table'],
  ['/Home/AuditLog', 'audit-table'],
];

for (const [url, testid] of pages) {
  test(`page ${url} renders ${testid}`, async ({ page }) => {
    const resp = await page.goto(url);
    expect(resp?.status()).toBe(200);
    await expect(page.getByTestId(testid)).toBeVisible();
  });
}

test('dashboard shows seeded KPIs', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByTestId('kpi-customers')).toBeVisible();
  const customers = await page.getByTestId('kpi-customers').innerText();
  expect(Number(customers)).toBeGreaterThanOrEqual(1);
});

test('general ledger is populated and balances at the footer', async ({ page }) => {
  await page.goto('/Home/GeneralLedger');
  const rows = page.locator('[data-testid="gl-table"] tbody tr');
  expect(await rows.count()).toBeGreaterThan(0);
  // footer totals row exists (debit/credit totals rendered)
  await expect(page.locator('[data-testid="gl-table"] tfoot')).toBeVisible();
});

test('dev-auth login switches the acting user', async ({ page }) => {
  await page.goto('/Home/Login');
  await expect(page.getByTestId('login-form')).toBeVisible();
  await page.fill('input[name="username"]', 'alice');
  await page.fill('input[name="roles"]', 'Sales User|Accounts User');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL('http://localhost:5081/');
  await expect(page.getByTestId('dashboard')).toBeVisible();
});

test('api health endpoint responds', async ({ request }) => {
  const resp = await request.get('/api/health');
  expect(resp.status()).toBe(200);
  expect(await resp.json()).toMatchObject({ status: 'ok', product: 'LAF Enterprise ERP GoldGenerated' });
});
