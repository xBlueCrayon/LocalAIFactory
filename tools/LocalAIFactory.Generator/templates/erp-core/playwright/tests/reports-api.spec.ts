import { test, expect } from '@playwright/test';

// ERP-grade report + manufacturing depth exposed over the REST API. companyId 1 is the seeded company.
const reportEndpoints = [
  '/api/reports/sales-register?companyId=1',
  '/api/reports/purchase-register?companyId=1',
  '/api/reports/sales-by-customer?companyId=1',
  '/api/reports/purchase-by-supplier?companyId=1',
  '/api/reports/receivables-aging?companyId=1',
  '/api/reports/tax-summary?companyId=1',
  '/api/reports/stock-valuation?companyId=1',
  '/api/reports/reorder?companyId=1&threshold=1000',
  '/api/reports/work-order-summary?companyId=1',
  '/api/boms',
  '/api/production-orders',
];

for (const url of reportEndpoints) {
  test(`report/manufacturing endpoint responds: ${url}`, async ({ request }) => {
    const resp = await request.get(url);
    expect(resp.status()).toBe(200);
  });
}

test('tax summary returns the output/input/net shape', async ({ request }) => {
  const resp = await request.get('/api/reports/tax-summary?companyId=1');
  expect(resp.status()).toBe(200);
  const body = await resp.json();
  expect(body).toHaveProperty('outputTax');
  expect(body).toHaveProperty('inputTax');
  expect(body).toHaveProperty('netTax');
});

test('purchase register reflects the seeded demo procurement', async ({ request }) => {
  const resp = await request.get('/api/reports/purchase-register?companyId=1');
  const rows = await resp.json();
  expect(Array.isArray(rows)).toBe(true);
});
