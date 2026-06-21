import { test, expect } from '@playwright/test';
import * as fs from 'fs';

const shots = 'screenshots';
fs.mkdirSync(shots, { recursive: true });
const PNG = 'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';

test('dashboard loads with Generate Client button and health', async ({ page }) => {
  const resp = await page.goto('/');
  expect(resp?.status()).toBe(200);
  await expect(page.getByTestId('generate-client-btn')).toBeVisible();
  await expect(page.getByTestId('clients-table')).toBeVisible();
  await expect(page.locator('#health')).toContainText('Running', { timeout: 10000 });
  await page.screenshot({ path: `${shots}/01-dashboard.png`, fullPage: true });
});

test('health endpoint and token are present', async ({ request }) => {
  const r = await request.get('/api/health');
  expect(r.status()).toBe(200);
  const j = await r.json();
  expect(j.status).toBe('ok');
  expect(String(j.token).length).toBeGreaterThan(8);
});

test('simulated client connects, dashboard shows frames, then disconnect', async ({ page }) => {
  await page.goto('/');
  // open a WebSocket from the browser, authenticate with the server token, stream a few fake frames
  await page.evaluate(async (png) => {
    const tok = await (await fetch('/api/health')).json();
    const ws = new WebSocket('ws://localhost:5090/stream');
    await new Promise<void>((res) => { ws.onopen = () => res(); });
    ws.send(JSON.stringify({ type: 'handshake', token: tok.token, displayName: 'PwClient', sessionId: 'pw1' }));
    for (let i = 1; i <= 6; i++) {
      ws.send(JSON.stringify({ type: 'frame', sessionId: 'pw1', frameNumber: i, width: 1, height: 1, contentType: 'image/png', dataBase64: png, tsUnixMs: Date.now() }));
    }
    (window as any).__ws = ws;
  }, PNG);

  // dashboard polls every 1s; wait for the row to appear in the UI
  await expect(page.getByTestId('clients-table')).toContainText('PwClient', { timeout: 8000 });
  await page.waitForTimeout(1200);
  // assert the same data the dashboard renders: the server counted the streamed frames
  const clients = await (await page.request.get('/api/clients')).json();
  const me = clients.find((c: any) => c.id === 'pw1');
  expect(me, 'pw1 session present').toBeTruthy();
  expect(me.frameCount, 'frames counted').toBeGreaterThan(0);
  await page.screenshot({ path: `${shots}/02-client-streaming.png`, fullPage: true });

  // disconnect from the server side
  await page.request.post('/api/disconnect/pw1');
  await page.waitForTimeout(1200);
  await expect(page.getByTestId('clients-table')).toContainText('stopped', { timeout: 8000 });
  await page.screenshot({ path: `${shots}/03-after-disconnect.png`, fullPage: true });
});

test('no HTTP 500 navigating the dashboard', async ({ page }) => {
  const bad: number[] = [];
  page.on('response', (r) => { if (r.status() >= 500) bad.push(r.status()); });
  await page.goto('/');
  await page.waitForTimeout(1500);
  expect(bad).toEqual([]);
});
