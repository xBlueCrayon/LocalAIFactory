import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests',
  timeout: 40000,
  fullyParallel: false,
  reporter: [['list']],
  use: { baseURL: 'http://localhost:5090', trace: 'off' },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project ../src/LafScreenStream.Server/LafScreenStream.Server.csproj -c Release --no-launch-profile',
    url: 'http://localhost:5090/api/health',
    reuseExistingServer: true,
    timeout: 120000,
    env: { LAFSS_NO_BROWSER: '1' },
  },
});
