import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  timeout: 30000, // デフォルトタイムアウトを30秒に設定
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    actionTimeout: 10000, // アクションのタイムアウトを10秒に設定
    navigationTimeout: 15000, // ナビゲーションのタイムアウトを15秒に設定
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: true, // 既存のサーバーがあれば再利用
    timeout: 120000, // 起動タイムアウトを2分に延長
    stdout: 'pipe',
  },
});
