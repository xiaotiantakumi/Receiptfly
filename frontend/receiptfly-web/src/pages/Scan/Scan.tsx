import { useState, useRef, useEffect } from 'react';
import { Camera, Image as ImageIcon, X, Edit, Upload, Trash2, Loader2, FileText } from 'lucide-react';
import styles from './Scan.module.css';
import { useNavigate } from 'react-router-dom';
import { useSettings } from '../../context/SettingsContext';
import { useReceipts } from '../../context/ReceiptContext';

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

interface BatchReceiptResult {
  fileName: string;
  success: boolean;
  receiptId?: string;
  receipt?: any;
  error?: string;
}

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
  const [ocrResults, setOcrResults] = useState<OcrResult[]>([]);
  const [receiptResults, setReceiptResults] = useState<BatchReceiptResult[]>([]);

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
    setOcrResults([]);
    setReceiptResults([]);

    try {
      // Step 1: OCR処理
      const formData = new FormData();
      capturedImages.forEach((image) => {
        formData.append('files', image.file);
      });

      const ocrResponse = await fetch(`${import.meta.env.VITE_API_BASE_URL}/ocr/batch`, {
        method: 'POST',
        body: formData,
      });

      if (!ocrResponse.ok) {
        throw new Error('OCR処理に失敗しました');
      }

      const ocrData = await ocrResponse.json();
      const ocrResults = ocrData.results || [];
      setOcrResults(ocrResults);

      // OCRエラーがある場合は処理を中断
      const ocrErrors = ocrResults.filter((r: OcrResult) => r.error || !r.text);
      if (ocrErrors.length > 0) {
        alert(`${ocrErrors.length}件の画像でOCR処理に失敗しました`);
        setIsUploading(false);
        return;
      }

      // Step 2: レシート作成処理
      setIsUploading(false);
      setIsCreatingReceipts(true);

      const accountTitles = settings.accountTitles.map(t => t.name);
      const categories = settings.categories.map(c => c.name);

      const batchRequest = {
        items: ocrResults.map((result: OcrResult) => ({
          ocrText: result.text || '',
          fileName: result.fileName,
          filePath: result.filePath
        })),
        accountTitles,
        categories
      };

      console.log('バッチリクエスト:', {
        itemsCount: batchRequest.items.length,
        accountTitlesCount: accountTitles.length,
        categoriesCount: categories.length,
        firstItemOcrTextLength: batchRequest.items[0]?.ocrText?.length || 0
      });

      const receiptResponse = await fetch(`${import.meta.env.VITE_API_BASE_URL}/receipts/batch-from-ocr`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(batchRequest),
      });

      if (!receiptResponse.ok) {
        let errorMessage = `レシート作成に失敗しました (${receiptResponse.status})`;
        try {
          const errorData = await receiptResponse.json();
          console.error('レシート作成エラー:', errorData);
          if (errorData.errors) {
            // ASP.NET Coreのバリデーションエラー
            const errorDetails = Object.entries(errorData.errors)
              .map(([key, value]: [string, any]) => `${key}: ${Array.isArray(value) ? value.join(', ') : value}`)
              .join('; ');
            errorMessage = `バリデーションエラー: ${errorDetails}`;
          } else if (errorData.error) {
            errorMessage = errorData.error;
          } else if (errorData.message) {
            errorMessage = errorData.message;
          }
        } catch (e) {
          const errorText = await receiptResponse.text();
          console.error('レシート作成エラー（JSON解析失敗）:', errorText);
          errorMessage = `レシート作成に失敗しました: ${errorText.substring(0, 200)}`;
        }
        throw new Error(errorMessage);
      }

      const receiptData = await receiptResponse.json();
      console.log('レシート作成レスポンス:', receiptData);
      const results = receiptData.results || [];
      setReceiptResults(results);
      
      // 結果をログに出力
      console.log('レシート作成結果:', {
        total: receiptData.total,
        succeeded: receiptData.succeeded,
        failed: receiptData.failed,
        results: results
      });


      // 結果を表示
      const succeeded = results.filter((r: BatchReceiptResult) => r.success).length;
      const failed = results.filter((r: BatchReceiptResult) => !r.success).length;

      // レシート一覧を再取得（ホーム画面に遷移する前に）
      try {
        await refreshReceipts();
        console.log('レシート一覧を再取得しました');
      } catch (refreshError) {
        console.error('レシート一覧の再取得に失敗しました:', refreshError);
      }

      if (failed > 0) {
        alert(`${succeeded}件のレシートを作成しました。${failed}件のレシート作成に失敗しました。`);
      } else {
        alert(`${succeeded}件のレシートを作成しました。`);
      }

      // 少し待ってからホーム画面に遷移（レシート一覧の更新を確実にするため）
      setTimeout(() => {
        navigate('/');
      }, 100);
    } catch (error) {
      console.error('アップロードエラー:', error);
      alert('画像のアップロードまたはレシート作成に失敗しました');
      setIsUploading(false);
      setIsCreatingReceipts(false);
    } finally {
      setIsUploading(false);
      setIsCreatingReceipts(false);
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
                  OCR処理中...
                </>
              ) : isCreatingReceipts ? (
                <>
                  <Loader2 size={18} className={styles.spinner} />
                  レシート作成中...
                </>
              ) : (
                <>
                  <Upload size={18} />
                  アップロード開始 ({capturedImages.length}枚)
                </>
              )}
            </button>
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
