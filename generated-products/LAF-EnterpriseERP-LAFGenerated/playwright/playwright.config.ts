import { defineConfig, devices } from '@playwright/test';

// Launches the generated ERP (SQLite-backed, no external services) and smoke-tests the UI.
export default defineConfig({
  testDir: './tests',
  timeout: 30000,
  fullyParallel: false,
  reporter: [['list'], ['json', { outputFile: 'playwright-report.json' }]],
  use: {
    baseURL: 'http://localhost:5081',
    trace: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project ../src/LafErp.Web/LafErp.Web.csproj -c Release --no-launch-profile --urls http://localhost:5081',
    url: 'http://localhost:5081/api/health',
    reuseExistingServer: true,
    timeout: 120000,
  },
});
