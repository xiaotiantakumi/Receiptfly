import { useState, useRef, useEffect } from 'react';
import { Camera, Image as ImageIcon, X, Edit, Upload, Trash2, Loader2, FileText } from 'lucide-react';
import styles from './Scan.module.css';
import { useNavigate } from 'react-router-dom';
import { useSettings } from '../../context/SettingsContext';
import { useReceipts } from '../../context/ReceiptContext';
import { uploadMultipleBlobs } from '../../services/blobUploadService';

interface CapturedImage {
  id: string;
  file: File;
  preview: string;
}

interface OcrResult {
  fileName: string;
  text?: string;
  filePath?: string;
  error?: string;
}

// BatchReceiptResult は現在未使用ですが、将来的に batch-from-ocr エンドポイントを使用する場合に必要になる可能性があります
// interface BatchReceiptResult {
//   fileName: string;
//   success: boolean;
//   receiptId?: string;
//   receipt?: any;
//   error?: string;
// }

export function Scan() {
  const navigate = useNavigate();
  const videoRef = useRef<HTMLVideoElement>(null);
  const streamRef = useRef<MediaStream | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const { settings } = useSettings();
  const { refreshReceipts } = useReceipts();
  const [capturedImages, setCapturedImages] = useState<CapturedImage[]>([]);
  const [isCameraActive, setIsCameraActive] = useState(false);
  const [isUploading, setIsUploading] = useState(false);
  const [isCreatingReceipts, setIsCreatingReceipts] = useState(false);
  const [uploadProgress, setUploadProgress] = useState<Record<string, number>>({});
  const [ocrResults, setOcrResults] = useState<OcrResult[]>([]);

  // カメラの初期化
  useEffect(() => {
    startCamera();
    return () => {
      stopCamera();
    };
  }, []);

  const startCamera = async () => {
    try {
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: 'environment' } // バックカメラを優先
      });
      streamRef.current = stream;
      if (videoRef.current) {
        videoRef.current.srcObject = stream;
        setIsCameraActive(true);
      }
    } catch (error) {
      console.error('カメラの起動に失敗しました:', error);
      setIsCameraActive(false);
    }
  };

  const stopCamera = () => {
    if (streamRef.current) {
      streamRef.current.getTracks().forEach(track => track.stop());
      streamRef.current = null;
    }
    if (videoRef.current) {
      videoRef.current.srcObject = null;
    }
    setIsCameraActive(false);
  };

  const captureImage = () => {
    if (!videoRef.current) return;

    const canvas = document.createElement('canvas');
    canvas.width = videoRef.current.videoWidth;
    canvas.height = videoRef.current.videoHeight;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    ctx.drawImage(videoRef.current, 0, 0);
    canvas.toBlob((blob) => {
      if (blob) {
        const file = new File([blob], `receipt-${Date.now()}.png`, { type: 'image/png' });
        const preview = URL.createObjectURL(blob);
        const newImage: CapturedImage = {
          id: Date.now().toString(),
          file,
          preview
        };
        setCapturedImages(prev => [...prev, newImage]);
      }
    }, 'image/png');
  };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    const files = e.target.files;
    if (!files) return;

    const newImages: CapturedImage[] = [];
    Array.from(files).forEach((file) => {
      if (file.type.startsWith('image/') || file.type === 'application/pdf') {
        const preview = URL.createObjectURL(file);
        newImages.push({
          id: Date.now().toString() + Math.random(),
          file,
          preview
        });
      }
    });
    setCapturedImages(prev => [...prev, ...newImages]);
    
    // ファイル入力のリセット
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const removeImage = (id: string) => {
    setCapturedImages(prev => {
      const image = prev.find(img => img.id === id);
      if (image) {
        URL.revokeObjectURL(image.preview);
      }
      return prev.filter(img => img.id !== id);
    });
  };

  const handleBatchUpload = async () => {
    if (capturedImages.length === 0) return;

    setIsUploading(true);
    setUploadProgress({});
    setOcrResults([]);

    try {
      const files = capturedImages.map(img => img.file);
      const accountTitles = settings.accountTitles.map(t => t.name);
      const categories = settings.categories.map(c => c.name);

      // Step 1: Blob Storageに直接アップロード
      console.log('Blob Storageへのアップロードを開始...');
      const uploadResults = await uploadMultipleBlobs(
        files,
        'receipt-images',
        (fileName, progress) => {
          setUploadProgress(prev => ({ ...prev, [fileName]: progress }));
        }
      );

      // アップロード失敗がある場合は処理を中断
      const failedUploads = uploadResults.filter(r => !r.success);
      if (failedUploads.length > 0) {
        const errorMessages = failedUploads.map(r => `${r.fileName}: ${r.error}`).join('\n');
        alert(`${failedUploads.length}件のファイルのアップロードに失敗しました:\n${errorMessages}`);
        setIsUploading(false);
        return;
      }

      console.log('アップロード完了:', uploadResults);

      // Step 2: OCR処理をキューに送信
      setIsUploading(false);
      setIsCreatingReceipts(true);

      const blobPaths = uploadResults.map(r => r.blobPath);
      const queueRequest = {
        blobPaths,
        accountTitles,
        categories
      };

      console.log('OCR処理をキューに送信:', queueRequest);

      const queueResponse = await fetch(`${import.meta.env.VITE_API_BASE_URL}/queue-ocr`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(queueRequest),
      });

      if (!queueResponse.ok) {
        const errorData = await queueResponse.json().catch(() => ({ error: 'Failed to queue OCR processing' }));
        throw new Error(errorData.error || `キューへの送信に失敗しました: ${queueResponse.status}`);
      }

      const queueData = await queueResponse.json();
      console.log('キュー送信完了:', queueData);

      // Step 3: 処理完了を待つ（ポーリング）
      // 簡易実装: 一定時間待機してからレシート一覧を更新
      // 本番環境では、WebSocketやSignalRを使用してリアルタイム通知を実装することを推奨
      await new Promise(resolve => setTimeout(resolve, 5000)); // 5秒待機

      // Step 4: レシート一覧を更新
      try {
        await refreshReceipts();
        console.log('レシート一覧を再取得しました');
      } catch (refreshError) {
        console.error('レシート一覧の再取得に失敗しました:', refreshError);
      }

      alert(`${uploadResults.length}件の画像をアップロードし、OCR処理を開始しました。処理完了後、レシート一覧に反映されます。`);

      // ホーム画面に遷移
      setTimeout(() => {
        navigate('/');
      }, 100);
    } catch (error) {
      console.error('アップロードエラー:', error);
      alert(`画像のアップロードまたはOCR処理の開始に失敗しました: ${error instanceof Error ? error.message : 'Unknown error'}`);
      setIsUploading(false);
      setIsCreatingReceipts(false);
    } finally {
      setIsUploading(false);
      setIsCreatingReceipts(false);
      setUploadProgress({});
    }
  };

  const handleClose = () => {
    stopCamera();
    navigate('/');
  };

  return (
    <div className={`${styles.container} animate-fade-in`}>
      <div className={styles.cameraView}>
        {isCameraActive ? (
          <video
            ref={videoRef}
            autoPlay
            playsInline
            className={styles.video}
          />
        ) : (
          <div className={styles.cameraPlaceholder}>
            <Camera size={48} className={styles.placeholderIcon} />
            <p>カメラを起動できませんでした</p>
          </div>
        )}
        
        <div className={styles.overlay}>
          <div className={styles.header}>
            <button className={styles.closeButton} onClick={handleClose}>
              <X size={24} />
            </button>
            <span className={styles.modeBadge}>レシートモード</span>
          </div>

          <div className={styles.controls}>
            <button 
              className={styles.galleryButton}
              onClick={() => fileInputRef.current?.click()}
            >
              <ImageIcon size={24} />
            </button>
            
            <button 
              className={styles.shutterButton}
              onClick={captureImage}
              disabled={!isCameraActive}
            >
              <div className={styles.shutterInner} />
            </button>
            
            <div className={styles.spacer} /> 
          </div>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          accept="image/*,.pdf"
          multiple
          onChange={handleFileSelect}
          style={{ display: 'none' }}
        />
      </div>
      
      <div className={styles.recentScans}>
        <div className={styles.recentHeader}>
          <h3 className={styles.recentTitle}>
            {capturedImages.length > 0 ? `選択中の画像 (${capturedImages.length})` : '最近のスキャン'}
          </h3>
          <button 
            className={styles.manualEntryButton}
            onClick={() => navigate('/manual-entry')}
          >
            <Edit size={18} />
            手動入力
          </button>
        </div>

        {capturedImages.length > 0 ? (
          <div className={styles.imagePreviewContainer}>
            <div className={styles.imageGrid}>
              {capturedImages.map((image) => (
                <div key={image.id} className={styles.imagePreview}>
                  {image.file.type === 'application/pdf' ? (
                    <div className={styles.pdfPreview}>
                      <FileText size={48} />
                      <span className={styles.pdfName}>{image.file.name}</span>
                    </div>
                  ) : (
                    <img src={image.preview} alt="Preview" />
                  )}
                  <button
                    className={styles.removeButton}
                    onClick={() => removeImage(image.id)}
                  >
                    <Trash2 size={16} />
                  </button>
                </div>
              ))}
            </div>
            <button
              className={styles.uploadButton}
              onClick={handleBatchUpload}
              disabled={isUploading || isCreatingReceipts}
            >
              {isUploading ? (
                <>
                  <Loader2 size={18} className={styles.spinner} />
                  アップロード中...
                </>
              ) : isCreatingReceipts ? (
                <>
                  <Loader2 size={18} className={styles.spinner} />
                  OCR処理開始中...
                </>
              ) : (
                <>
                  <Upload size={18} />
                  アップロード開始 ({capturedImages.length}枚)
                </>
              )}
            </button>
            {Object.keys(uploadProgress).length > 0 && (
              <div className={styles.progressContainer}>
                {Object.entries(uploadProgress).map(([fileName, progress]) => (
                  <div key={fileName} className={styles.progressItem}>
                    <span className={styles.progressFileName}>{fileName}</span>
                    <div className={styles.progressBar}>
                      <div 
                        className={styles.progressFill} 
                        style={{ width: `${progress}%` }}
                      />
                    </div>
                    <span className={styles.progressPercent}>{Math.round(progress)}%</span>
                  </div>
                ))}
              </div>
            )}
            {ocrResults.length > 0 && (
              <div className={styles.ocrResults}>
                <h4>OCR結果</h4>
                {ocrResults.map((result, index) => (
                  <div key={index} className={styles.ocrResultItem}>
                    <strong>{result.fileName}</strong>
                    {result.text ? (
                      <pre className={styles.ocrText}>{result.text}</pre>
                    ) : (
                      <p className={styles.ocrError}>{result.error}</p>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        ) : (
          <div className={styles.emptyState}>
            まだスキャンされたレシートはありません
          </div>
        )}
      </div>
    </div>
  );
}
