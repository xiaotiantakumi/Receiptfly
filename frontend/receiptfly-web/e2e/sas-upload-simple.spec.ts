import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

test.describe('SAS Token Upload Test (Simple)', () => {
  test('should get SAS token and upload file', async ({ page, request }) => {
    test.setTimeout(20000); // 20秒タイムアウト
    
    const apiBaseUrl = 'http://localhost:7071/api';
    
    // 1. SASトークン取得のテスト
    console.log('Step 1: Getting SAS token...');
    const response = await request.get(`${apiBaseUrl}/getSas?containerName=receipt-images&blobName=test-upload.pdf`, {
      timeout: 10000
    });
    
    expect(response.status()).toBe(200);
    const data = await response.json();
    
    expect(data).toHaveProperty('sasUrl');
    expect(data).toHaveProperty('containerName', 'receipt-images');
    expect(data).toHaveProperty('blobName', 'test-upload.pdf');
    expect(data.sasUrl).toContain('http://127.0.0.1:10000');
    expect(data.sasUrl).toContain('sv=');
    expect(data.sasUrl).toContain('sig=');
    
    console.log('✓ SAS Token obtained:', data.sasUrl.substring(0, 80) + '...');

    // 2. SASトークンを使ってファイルをアップロード
    console.log('Step 2: Uploading file using SAS token...');
    const sasUrl = data.sasUrl;
    const blobName = `test-upload-${Date.now()}.pdf`;
    
    // 新しいSASトークンを取得
    const sasResponse2 = await request.get(`${apiBaseUrl}/getSas?containerName=receipt-images&blobName=${blobName}`, {
      timeout: 10000
    });
    const sasData2 = await sasResponse2.json();
    const sasUrl2 = sasData2.sasUrl;
    
    // テスト用PDFファイルを読み込む
    const testFilePath = path.join(process.cwd(), 'backend/Receiptfly.Api.Tests/data/20241222_data.pdf');
    
    let fileData: Buffer;
    if (fs.existsSync(testFilePath)) {
      fileData = fs.readFileSync(testFilePath);
      console.log('✓ Test file found:', testFilePath, `(${fileData.length} bytes)`);
    } else {
      // ファイルが存在しない場合は小さなテストデータを作成
      console.log('⚠ Test file not found, creating dummy PDF data');
      fileData = Buffer.from([0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34]); // PDF header
    }
    
    // PUTリクエストでアップロード
    const uploadResponse = await request.put(sasUrl2, {
      headers: {
        'x-ms-blob-type': 'BlockBlob',
        'Content-Type': 'application/pdf',
      },
      data: fileData,
      timeout: 15000
    });
    
    expect(uploadResponse.status()).toBe(201);
    console.log('✓ File uploaded successfully! Status:', uploadResponse.status());
  });
});



