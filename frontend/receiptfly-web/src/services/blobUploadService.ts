import { BlockBlobClient } from '@azure/storage-blob';
import { generateReceiptId } from '../utils/idGenerator';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:7071/api';

export interface UploadResult {
  blobPath: string;
  fileName: string;
  success: boolean;
  error?: string;
}

export interface SasTokenResponse {
  sasUrl: string;
  containerName: string;
  blobName?: string;
  expiresOn: string;
}

/**
 * SASトークンを取得
 */
export async function getSasToken(containerName: string, blobName?: string): Promise<string> {
  const params = new URLSearchParams({ containerName });
  if (blobName) {
    params.append('blobName', blobName);
  }

  const response = await fetch(`${API_BASE_URL}/getSas?${params.toString()}`);
  
  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Failed to get SAS token' }));
    throw new Error(error.error || `Failed to get SAS token: ${response.status}`);
  }

  const data: SasTokenResponse = await response.json();
  return data.sasUrl;
}

/**
 * Blob Storageにファイルをアップロード
 */
export async function uploadBlob(
  file: File,
  sasUrl: string,
  onProgress?: (progress: number) => void,
  metadata?: Record<string, string>
): Promise<void> {
  const blockBlobClient = new BlockBlobClient(sasUrl);

  await blockBlobClient.uploadData(file, {
    blobHTTPHeaders: {
      blobContentType: file.type,
    },
    metadata: metadata,
    onProgress: (ev) => {
      if (onProgress && file.size > 0) {
        const progress = (ev.loadedBytes / file.size) * 100;
        onProgress(progress);
      }
    },
  });
}

/**
 * 複数ファイルを一括アップロード
 */
export async function uploadMultipleBlobs(
  files: File[],
  containerName: string,
  onProgress?: (fileName: string, progress: number) => void
): Promise<UploadResult[]> {
  const results: UploadResult[] = [];

  for (const file of files) {
    try {
      // レシートIDを生成
      const receiptId = generateReceiptId();
      
      // 元のファイル名から拡張子を取得
      const extension = file.name.split('.').pop() || '';
      
      // ファイル名を receipt-{uuid}.{ext} 形式に変更
      const blobName = extension ? `${receiptId}.${extension}` : receiptId;

      // SASトークンを取得
      const sasUrl = await getSasToken(containerName, blobName);

      // メタデータに元のファイル名を保存
      const metadata = {
        original_filename: file.name,
      };

      // アップロード
      await uploadBlob(file, sasUrl, (progress) => {
        onProgress?.(file.name, progress);
      }, metadata);

      // blobPathは後続処理で使用するため、コンテナ名/ファイル名の形式で返す
      results.push({
        blobPath: `${containerName}/${blobName}`,
        fileName: file.name,
        success: true,
      });
    } catch (error) {
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      results.push({
        blobPath: '',
        fileName: file.name,
        success: false,
        error: errorMessage,
      });
    }
  }

  return results;
}

