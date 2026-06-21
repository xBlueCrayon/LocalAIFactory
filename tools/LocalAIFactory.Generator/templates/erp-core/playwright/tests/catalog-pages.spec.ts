import { test, expect } from '@playwright/test';

// Each deterministic spec-driven module exposes a working list page (create/edit/deactivate UI).
const ENTITIES = [
  'Quotation', 'DeliveryNote', 'CreditNote', 'PurchaseReceipt', 'MaterialRequest',
  'DebitNote', 'StockTransfer', 'PriceList', 'WorkOrder', 'JobCard',
  'QualityInspection', 'Employee', 'LeaveApplication', 'Timesheet', 'WebProduct',
];

for (const entity of ENTITIES) {
  test(`catalog list page renders for ${entity}`, async ({ page }) => {
    const resp = await page.goto(`/Catalog/List?entity=${entity}`);
    expect(resp?.status()).toBe(200);
    await expect(page.getByTestId('record-table')).toBeVisible();
    await expect(page.getByTestId('create-link')).toBeVisible();
  });
}
