import { defineConfig } from '@playwright/test'

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: process.env.DASHBOARD_URL || 'http://localhost:13001',
    browserName: 'chromium',
    headless: true,
    launchOptions: {
      executablePath: process.env.CHROME_EXECUTABLE || '/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',
    },
    trace: 'retain-on-failure',
  },
  reporter: [['line'], ['html', { open: 'never' }]],
})
