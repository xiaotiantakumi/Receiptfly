import { BlockBlobClient } from '@azure/storage-blob';

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
  onProgress?: (progress: number) => void
): Promise<void> {
  const blockBlobClient = new BlockBlobClient(sasUrl);

  await blockBlobClient.uploadData(file, {
    blobHTTPHeaders: {
      blobContentType: file.type,
    },
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
      // ファイル名にタイムスタンプを追加して一意性を確保
      const timestamp = Date.now();
      const uniqueFileName = `${timestamp}-${file.name}`;
      // blobNameはコンテナ名を含まないファイル名のみ
      const blobName = uniqueFileName;

      // SASトークンを取得
      const sasUrl = await getSasToken(containerName, blobName);

      // アップロード
      await uploadBlob(file, sasUrl, (progress) => {
        onProgress?.(file.name, progress);
      });

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

