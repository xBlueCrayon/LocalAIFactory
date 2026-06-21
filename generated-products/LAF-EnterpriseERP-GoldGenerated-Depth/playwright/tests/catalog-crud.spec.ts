import { test, expect } from '@playwright/test';

// Full master-data lifecycle through the generated UI: create -> appears in list -> edit -> deactivate.
const ENTITY = 'Quotation';

test('catalog overview lists generated modules with view/edit links', async ({ page }) => {
  await page.goto('/Catalog');
  await expect(page.getByTestId('catalog-table')).toBeVisible();
  await expect(page.getByTestId(`list-${ENTITY}`)).toBeVisible();
});

test('create a catalog record via the UI form', async ({ page }) => {
  await page.goto(`/Catalog/Create?entity=${ENTITY}`);
  await expect(page.getByTestId('create-form')).toBeVisible();
  await page.getByTestId('field-Name').fill('PW Lifecycle Quote');
  await page.getByTestId('create-submit').click();
  await page.goto(`/Catalog/List?entity=${ENTITY}`);
  await expect(page.getByTestId('record-table')).toContainText('PW Lifecycle Quote');
});

test('edit a catalog record via the UI form', async ({ page }) => {
  await page.goto(`/Catalog/List?entity=${ENTITY}`);
  await page.locator('[data-testid^="edit-"]').first().click();
  await expect(page.getByTestId('edit-form')).toBeVisible();
  await page.getByTestId('field-Name').fill('PW Edited Quote');
  await page.getByTestId('edit-submit').click();
  await page.goto(`/Catalog/List?entity=${ENTITY}`);
  await expect(page.getByTestId('record-table')).toContainText('PW Edited Quote');
});

test('deactivate (soft-delete) a catalog record', async ({ page }) => {
  await page.goto(`/Catalog/List?entity=${ENTITY}`);
  const before = await page.locator('[data-testid="record-table"] tbody tr').count();
  await page.locator('[data-testid^="deactivate-"]').first().click();
  await page.goto(`/Catalog/List?entity=${ENTITY}`);
  const after = await page.locator('[data-testid="record-table"] tbody tr').count();
  expect(after).toBeLessThan(before);
});
