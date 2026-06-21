import { test, expect } from '@playwright/test';
import * as fs from 'fs';

// Opens the generated ERP in a real browser, logs in via dev-auth, navigates every available page,
// asserts none return HTTP 500, and captures a screenshot of each.
const shotDir = 'screenshots';
fs.mkdirSync(shotDir, { recursive: true });

const pages: [string, string, string][] = [
  ['/', 'dashboard', '01-dashboard'],
  ['/Home/Customers', 'customers-table', '02-customers'],
  ['/Home/Items', 'items-table', '03-items'],
  ['/Home/SalesInvoices', 'sales-invoices-table', '04-sales-invoices'],
  ['/Home/GeneralLedger', 'gl-table', '05-general-ledger'],
  ['/Home/StockBalance', 'stock-balance-table', '06-stock-balance'],
  ['/Home/WorkflowInbox', 'workflow-table', '07-workflow-inbox'],
  ['/Home/AuditLog', 'audit-table', '08-audit-log'],
  ['/Catalog', 'catalog-table', '09-catalog-generated'],
];

test('login then navigate every page with no HTTP 500, capturing screenshots', async ({ page }) => {
  // 1. Dashboard loads
  let resp = await page.goto('/');
  expect(resp?.status()).toBe(200);
  await expect(page.getByTestId('dashboard')).toBeVisible();
  await page.screenshot({ path: `${shotDir}/00-home.png`, fullPage: true });

  // 2. Login as admin / System Manager (dev auth)
  resp = await page.goto('/Home/Login');
  expect(resp?.status()).toBe(200);
  await page.fill('input[name="username"]', 'admin');
  await page.fill('input[name="roles"]', 'System Manager');
  await page.click('button[type="submit"]');
  await expect(page).toHaveURL('http://localhost:5081/');
  await page.screenshot({ path: `${shotDir}/00b-logged-in.png`, fullPage: true });

  // 3. Navigate every page, assert 200 + element visible + screenshot
  for (const [url, testid, shot] of pages) {
    const r = await page.goto(url);
    expect(r?.status(), `page ${url} must not 500`).toBe(200);
    await expect(page.getByTestId(testid)).toBeVisible();
    await page.screenshot({ path: `${shotDir}/${shot}.png`, fullPage: true });
  }

  // 4. API health confirms product identity
  const health = await page.request.get('/api/health');
  expect(health.status()).toBe(200);
  expect(await health.json()).toMatchObject({ status: 'ok' });
});
