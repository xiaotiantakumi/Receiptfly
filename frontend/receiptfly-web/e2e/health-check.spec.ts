import { test, expect } from '@playwright/test';

test('frontend health check', async ({ page }) => {
  const frontendUrl = 'http://localhost:5173';
  
  console.log(`Checking frontend at ${frontendUrl}...`);
  
  try {
    // フロントエンドにアクセス
    const response = await page.goto(frontendUrl, { waitUntil: 'domcontentloaded', timeout: 30000 });
    
    if (response) {
      console.log(`Frontend status: ${response.status()}`);
      expect(response.status()).toBe(200);
    } else {
      throw new Error('No response received');
    }
    
    // タイトルなどの要素を確認
    const title = await page.title();
    console.log(`Page title: ${title}`);
    
    // body要素が存在することを確認
    await expect(page.locator('body')).toBeVisible();
    console.log('Frontend is running and visible');
    
  } catch (error) {
    console.error('Frontend check failed:', error);
    throw error;
  }
});

