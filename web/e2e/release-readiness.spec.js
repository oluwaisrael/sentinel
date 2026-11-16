import { test, expect } from '@playwright/test'

test('registers, logs in, manages a monitor, and loads history panels', async ({ page }) => {
  const email = `release-${Date.now()}@example.com`
  const password = 'Strong-password-123!'

  await page.goto('/')
  await expect(page.getByRole('heading', { name: 'Know before your users do.' })).toBeVisible()
  await page.getByRole('button', { name: 'Create account' }).click()
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Create workspace' }).click()
  await expect(page.getByRole('heading', { name: 'Good morning, operator.' })).toBeVisible()
  await expect(page.getByText('0 endpoints')).toBeVisible()

  await page.getByLabel('Name').fill('Release monitor')
  await page.getByLabel('URL').fill('https://example.com/health')
  await page.getByRole('button', { name: 'Start monitoring' }).click()
  await expect(page.getByRole('heading', { name: 'Release monitor' })).toBeVisible()
  await expect(page.getByText('Recent checks', { exact: true })).toBeVisible()
  await expect(page.getByText('Recovery history', { exact: true })).toBeVisible()

  await page.getByLabel('Name').fill('Release monitor updated')
  await page.getByRole('button', { name: 'Save changes' }).click()
  await expect(page.getByRole('heading', { name: 'Release monitor updated' })).toBeVisible()

  page.once('dialog', dialog => dialog.accept())
  await page.getByRole('button', { name: 'Delete' }).click()
  await expect(page.getByText('0 endpoints')).toBeVisible()

  await page.getByLabel('Name').fill('History monitor')
  await page.getByLabel('URL').fill('https://example.com/health')
  await page.getByRole('button', { name: 'Start monitoring' }).click()
  await expect(page.getByText('No checks recorded in this window.')).toBeVisible()
  await expect(page.getByText('No incidents in the last 31 days.')).toBeVisible()

  await page.getByRole('button', { name: 'Sign out' }).click()
  await expect(page.getByRole('heading', { name: 'Know before your users do.' })).toBeVisible()
  await page.getByLabel('Email').fill(email)
  await page.getByLabel('Password').fill(password)
  await page.getByRole('button', { name: 'Open command center' }).click()
  await expect(page.getByText('History monitor')).toBeVisible()
})
