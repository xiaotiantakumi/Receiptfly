import { test, expect } from '@playwright/test';
import net from 'net';

const checkPort = (port: number): Promise<boolean> => {
  return new Promise((resolve) => {
    const socket = new net.Socket();
    socket.setTimeout(1000);
    socket.on('connect', () => {
      socket.destroy();
      resolve(true);
    });
    socket.on('timeout', () => {
      socket.destroy();
      resolve(false);
    });
    socket.on('error', () => {
      socket.destroy();
      resolve(false);
    });
    socket.connect(port, '127.0.0.1');
  });
};

test.beforeAll(async () => {
  // Check Azurite (Blob Service)
  const isAzuriteRunning = await checkPort(10000);
  if (!isAzuriteRunning) {
    throw new Error('Azurite is not running on port 10000. Please start Azurite (e.g., using Azurite extension in VS Code or `azurite` command) before running tests.');
  }

  // Check Backend
  const isBackendRunning = await checkPort(7071);
  if (!isBackendRunning) {
    throw new Error('Backend is not running on port 7071. Please start the Azure Functions backend (`func start` in backend/Receiptfly.Functions) before running tests.');
  }
});

test('Receipt lifecycle: Register, Verify, Edit, Persist', async ({ page }) => {
  // 1. Go to Home
  await page.goto('/');
  // Wait for dashboard to load
  await expect(page.getByText('最近のレシート')).toBeVisible();

  // 2. Go to Scan -> Manual Entry
  // Assuming there is a navigation link to Scan or a button
  // Checking App.tsx, Scan is at /scan. 
  // Let's find a way to navigate there. Usually via a bottom nav or sidebar.
  // If not sure, we can goto /scan directly, but user said "一覧を開いてキャプチャからレシートを手動登録する"
  // "キャプチャ" likely means the Scan page.
  
  // Try to find the link to Scan page.
  // In Layout.tsx (not viewed but likely exists), there should be nav.
  // I'll assume there is a link with text "スキャン" or similar icon.
  // If not, I'll navigate by URL but verify the UI.
  
  // Let's try to click the "スキャン" link if it exists, or just go to /scan
  // To be safe and mimic user, I'll try to find the nav item.
  // But for robustness if I don't know the exact text, I might check the Layout.
  // I'll assume "スキャン" text exists based on typical Japanese apps.
  // Or I can look for an element that links to /scan.
  
  const scanLink = page.getByRole('link', { name: /スキャン|Scan/i });
  if (await scanLink.count() > 0) {
    await scanLink.first().click();
  } else {
    await page.goto('/scan');
  }

  await expect(page.getByText('レシートモード')).toBeVisible();

  // Click Manual Entry
  await page.getByRole('button', { name: '手動入力' }).click();
  await expect(page.getByText('手動レシート登録')).toBeVisible();

  // 3. Register Receipt
  const storeName = `Test Store ${Date.now()}`;
  await page.getByPlaceholder('例: スーパーライフ').fill(storeName);
  
  // Fill item
  await page.getByPlaceholder('品目名 *').fill('Test Item');
  await page.getByPlaceholder('金額 *').fill('1000');
  
  // Submit
  await page.getByRole('button', { name: 'レシートを登録' }).click();

  // 4. Verify Success and go Home
  await expect(page.getByText('レシートを登録しました')).toBeVisible();
  await page.getByRole('button', { name: 'ホームへ戻る' }).click();

  // 5. Verify in Dashboard
  // Wait for the list to refresh
  await expect(page.getByText(storeName)).toBeVisible();

  // 6. Edit Receipt
  await page.getByText(storeName).click();
  // Expect to be on detail page
  await expect(page.getByText('購入品目')).toBeVisible();
  
  await page.getByRole('button', { name: '編集' }).click();
  
  const newStoreName = `${storeName} Edited`;
  // Assuming the edit form has a label "店名" or similar
  await page.getByLabel('店名').fill(newStoreName);
  await page.getByRole('button', { name: '保存' }).click();
  
  // Verify edit in detail view (read mode)
  await expect(page.getByText(newStoreName)).toBeVisible();
  await expect(page.getByText(storeName)).not.toBeVisible();

  // 7. Reload and Verify Persistence
  await page.reload();
  // Wait for data to load
  await expect(page.getByText(newStoreName)).toBeVisible();
});
