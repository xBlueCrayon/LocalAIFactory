import { test, expect } from '@playwright/test';

// Proves the generated create/edit UI form (added in V5) actually persists a record end-to-end.
test('create a generated catalog record via the UI form', async ({ page }) => {
  const resp = await page.goto('/Catalog');
  expect(resp?.status()).toBe(200);
  await expect(page.getByTestId('catalog-table')).toBeVisible();

  // open the first available create form
  const firstCreate = page.locator('[data-testid^="create-"]').first();
  await expect(firstCreate).toBeVisible();
  const href = await firstCreate.getAttribute('href');
  const entity = (href || '').split('entity=')[1] || '';
  await firstCreate.click();

  await expect(page.getByTestId('create-form')).toBeVisible();
  await page.getByTestId('field-Name').fill('PW-Created-1');
  await page.getByTestId('create-submit').click();

  // redirected back to the catalog list
  await expect(page).toHaveURL(/\/Catalog$/);
  // verify via the module API that the record was persisted
  const route = (entity + 's').toLowerCase().replace('ss', 'ss');
  const r = await page.request.get('/api/catalog/' + (entity.toLowerCase().endsWith('s') ? entity.toLowerCase() + 'es' : entity.toLowerCase() + 's'));
  expect(r.status()).toBe(200);
  const rows = await r.json();
  expect(rows.some((x: any) => x.name === 'PW-Created-1'), 'created record present via API').toBeTruthy();
});
